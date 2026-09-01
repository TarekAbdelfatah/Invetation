using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class DelegationService : IDelegationService
    {
        private readonly IbtikarDbContext _db;

        public DelegationService(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<DelegationValidationResult> ValidateNewAsync(
            Guid innovationCommitteeId,
            Guid delegateMemberUserId,
            DateTime startAt,
            DateTime endAt,
            CancellationToken ct)
        {
            if (startAt >= endAt)
            {
                return DelegationValidationResult.Fail("يجب أن يكون تاريخ النهاية بعد تاريخ البداية.");
            }

            var delegateUser = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == delegateMemberUserId, ct);
            if (delegateUser is null)
            {
                return DelegationValidationResult.Fail("المستخدم المنسوب إليه غير موجود.");
            }

            var isHead = await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == innovationCommitteeId
                               && m.UserId == delegateMemberUserId
                               && m.IsHead, ct);
            if (isHead)
            {
                return DelegationValidationResult.Fail("لا يمكن تفويض رئيس اللجنة.");
            }

            var isMember = await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == innovationCommitteeId
                               && m.UserId == delegateMemberUserId, ct);
            if (!isMember)
            {
                return DelegationValidationResult.Fail("يجب أن يكون المفوَّض له عضوًا في اللجنة.");
            }

            var hasOverlap = await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.InnovationCommitteeId == innovationCommitteeId
                               && d.DelegateMemberUserId == delegateMemberUserId
                               && d.StartAt < endAt
                               && d.EndAt > startAt, ct);
            if (hasOverlap)
            {
                return DelegationValidationResult.Fail("يوجد تفويض قائم لهذا العضو يتداخل مع الفترة المختارة.");
            }

            return DelegationValidationResult.Success();
        }

        public Task<CommitteeDelegation?> GetActiveAsync(Guid innovationCommitteeId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.CommitteeDelegations.AsNoTracking()
                .FirstOrDefaultAsync(d => d.InnovationCommitteeId == innovationCommitteeId
                                          && d.StartAt <= now
                                          && d.EndAt >= now, ct);
        }

        public Task<bool> IsDelegateAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.DelegateMemberUserId == userId && d.StartAt <= now && d.EndAt >= now, ct);
        }

        public Task<bool> HasActiveDelegationAsync(Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct)
        {
            return _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.DelegateMemberUserId == delegateMemberUserId
                               && d.StartAt < endAt
                               && d.EndAt > startAt, ct);
        }

        public async Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
        {
            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct)
        {
            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.IsHead && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct)
        {
            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.InnovationCommitteeId == committeeId && !m.IsHead && m.User != null)
                .OrderBy(m => m.User!.FullName)
                .Select(m => new DelegationMemberOptionDto(m.UserId, m.User!.FullName, m.User!.Username))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<DelegationRowDto>> GetDelegationsAsync(Guid committeeId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return await _db.CommitteeDelegations.AsNoTracking()
                .Where(d => d.InnovationCommitteeId == committeeId)
                .OrderByDescending(d => d.StartAt)
                .Select(d => new DelegationRowDto(
                    d.Id,
                    d.DelegateMember != null ? d.DelegateMember.FullName : "—",
                    d.StartAt,
                    d.EndAt,
                    d.StartAt <= now && d.EndAt >= now))
                .ToListAsync(ct);
        }

        public async Task<DelegationCreateResultDto> AddAsync(
            Guid committeeId,
            Guid headUserId,
            Guid delegateMemberUserId,
            DateTime startAt,
            DateTime endAt,
            CancellationToken ct)
        {
            var isHead = await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.UserId == headUserId
                               && m.IsHead, ct);
            if (!isHead)
                return new(false, "أنت لست رئيس اللجنة ولا يمكنك التفويض.");

            var existingActive = await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.InnovationCommitteeId == committeeId && d.StartAt <= endAt && d.EndAt >= startAt, ct);
            if (existingActive)
                return new(false, "يوجد تفويض قائم متداخل مع هذه الفترة. لا يمكن إنشاء أكثر من تفويض واحد للجنة.");

            var validation = await ValidateNewAsync(committeeId, delegateMemberUserId, startAt, endAt, ct);
            if (!validation.Ok)
                return new(false, validation.Error ?? "تعذر التحقق من التفويض.");

            _db.CommitteeDelegations.Add(new CommitteeDelegation
            {
                Id = Guid.NewGuid(),
                InnovationCommitteeId = committeeId,
                HeadUserId = headUserId,
                DelegateMemberUserId = delegateMemberUserId,
                StartAt = startAt,
                EndAt = endAt,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return new(true, "تم إنشاء التفويض.");
        }

        public async Task<DelegationCreateResultDto> CancelAsync(Guid committeeId, Guid headUserId, Guid delegationId, CancellationToken ct)
        {
            var isHead = await _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.InnovationCommitteeId == committeeId
                               && m.UserId == headUserId
                               && m.IsHead, ct);
            if (!isHead)
                return new(false, "أنت لست رئيس اللجنة ولا يمكنك إلغاء التفويض.");

            var delegation = await _db.CommitteeDelegations
                .FirstOrDefaultAsync(d => d.Id == delegationId && d.InnovationCommitteeId == committeeId, ct);
            if (delegation is null)
                return new(false, "التفويض غير موجود.");

            _db.CommitteeDelegations.Remove(delegation);
            await _db.SaveChangesAsync(ct);

            return new(true, "تم إلغاء التفويض.");
        }
    }
}
