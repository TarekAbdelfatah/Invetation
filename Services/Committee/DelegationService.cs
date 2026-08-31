using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Committee
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
    }
}
