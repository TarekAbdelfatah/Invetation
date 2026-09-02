using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Services.Implementations
{
    public sealed class DelegationService : IDelegationService
    {
        private readonly IUserRepository _users;
        private readonly ICommitteeRepository _committees;
        private readonly IDelegationRepository _delegations;

        public DelegationService(
            IUserRepository users,
            ICommitteeRepository committees,
            IDelegationRepository delegations)
        {
            _users = users;
            _committees = committees;
            _delegations = delegations;
        }

        public async Task<DelegationValidationResult> ValidateNewAsync(
            Guid innovationCommitteeId,
            Guid delegateMemberUserId,
            DateTime startAt,
            DateTime endAt,
            CancellationToken ct)
        {
            startAt = startAt.FromKsaLocal();
            endAt = endAt.FromKsaLocal();

            if (startAt >= endAt)
            {
                return DelegationValidationResult.Fail("يجب أن يكون تاريخ النهاية بعد تاريخ البداية.");
            }

            var delegateExists = await _users.ExistsAsync(delegateMemberUserId, ct);
            if (!delegateExists)
            {
                return DelegationValidationResult.Fail("المستخدم المنسوب إليه غير موجود.");
            }

            var isHead = await _committees.IsHeadAsync(innovationCommitteeId, delegateMemberUserId, ct);
            if (isHead)
            {
                return DelegationValidationResult.Fail("لا يمكن تفويض رئيس اللجنة.");
            }

            var isMember = await _committees.IsMemberAsync(innovationCommitteeId, delegateMemberUserId, ct);
            if (!isMember)
            {
                return DelegationValidationResult.Fail("يجب أن يكون المفوَّض له عضوًا في اللجنة.");
            }

            var hasOverlap = await _delegations.HasOverlapAsync(innovationCommitteeId, delegateMemberUserId, startAt, endAt, ct);
            if (hasOverlap)
            {
                return DelegationValidationResult.Fail("يوجد تفويض قائم لهذا العضو يتداخل مع الفترة المختارة.");
            }

            return DelegationValidationResult.Success();
        }

        public Task<CommitteeDelegation?> GetActiveAsync(Guid innovationCommitteeId, CancellationToken ct)
            => _delegations.GetActiveAsync(innovationCommitteeId, ct);

        public Task<bool> IsDelegateAsync(Guid userId, CancellationToken ct)
            => _delegations.IsDelegateAsync(userId, ct);

        public Task<bool> HasActiveDelegationAsync(Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct)
            => _delegations.HasActiveDelegationAsync(delegateMemberUserId, startAt, endAt, ct);

        public Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
            => _committees.GetCommitteeIdForMemberAsync(userId, ct);

        public Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct)
            => _committees.GetCommitteeIdForHeadAsync(userId, ct);

        public Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct)
            => _committees.GetDelegateCandidatesAsync(committeeId, ct);

        public Task<IReadOnlyList<DelegationRowDto>> GetDelegationsAsync(Guid committeeId, CancellationToken ct)
            => _delegations.GetDelegationsAsync(committeeId, ct);

        public async Task<DelegationCreateResultDto> AddAsync(
            Guid committeeId,
            Guid headUserId,
            Guid delegateMemberUserId,
            DateTime startAt,
            DateTime endAt,
            CancellationToken ct)
        {
            var isHead = await _committees.IsHeadAsync(committeeId, headUserId, ct);
            if (!isHead)
                return new(false, "أنت لست رئيس اللجنة ولا يمكنك التفويض.");

            var existingActive = await _delegations.HasCommitteeOverlapAsync(committeeId, startAt.FromKsaLocal(), endAt.FromKsaLocal(), ct);
            if (existingActive)
                return new(false, "يوجد تفويض قائم متداخل مع هذه الفترة. لا يمكن إنشاء أكثر من تفويض واحد للجنة.");

            var validation = await ValidateNewAsync(committeeId, delegateMemberUserId, startAt, endAt, ct);
            if (!validation.Ok)
                return new(false, validation.Error ?? "تعذر التحقق من التفويض.");

            var utcStart = startAt.FromKsaLocal();
            var utcEnd = endAt.FromKsaLocal();

            await _delegations.AddAsync(new CommitteeDelegation
            {
                Id = Guid.NewGuid(),
                InnovationCommitteeId = committeeId,
                HeadUserId = headUserId,
                DelegateMemberUserId = delegateMemberUserId,
                StartAt = utcStart,
                EndAt = utcEnd,
                CreatedAt = DateTime.UtcNow
            }, ct);

            return new(true, "تم إنشاء التفويض.");
        }

        public async Task<DelegationCreateResultDto> CancelAsync(Guid committeeId, Guid headUserId, Guid delegationId, CancellationToken ct)
        {
            var isHead = await _committees.IsHeadAsync(committeeId, headUserId, ct);
            if (!isHead)
                return new(false, "أنت لست رئيس اللجنة ولا يمكنك إلغاء التفويض.");

            var delegation = await _delegations.GetByIdAsync(committeeId, delegationId, ct);
            if (delegation is null)
                return new(false, "التفويض غير موجود.");

            await _delegations.RemoveAndSaveAsync(delegation, ct);

            return new(true, "تم إلغاء التفويض.");
        }
    }
}