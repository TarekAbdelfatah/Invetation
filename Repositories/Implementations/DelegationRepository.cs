using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class DelegationRepository : IDelegationRepository
    {
        private readonly IbtikarDbContext _db;

        public DelegationRepository(IbtikarDbContext db) => _db = db;

        public async Task<bool> HasOverlapAsync(Guid committeeId, Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct)
            => await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.InnovationCommitteeId == committeeId
                               && d.DelegateMemberUserId == delegateMemberUserId
                               && d.StartAt < endAt
                               && d.EndAt > startAt, ct);

        public async Task<bool> HasCommitteeOverlapAsync(Guid committeeId, DateTime startAt, DateTime endAt, CancellationToken ct)
            => await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.InnovationCommitteeId == committeeId
                               && d.StartAt <= endAt
                               && d.EndAt >= startAt, ct);

        public async Task<CommitteeDelegation?> GetActiveAsync(Guid committeeId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return await _db.CommitteeDelegations.AsNoTracking()
                .FirstOrDefaultAsync(d => d.InnovationCommitteeId == committeeId
                                          && d.StartAt <= now
                                          && d.EndAt >= now, ct);
        }

        public async Task<bool> IsDelegateAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.DelegateMemberUserId == userId && d.StartAt <= now && d.EndAt >= now, ct);
        }

        public async Task<bool> HasActiveDelegationAsync(Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct)
            => await _db.CommitteeDelegations.AsNoTracking()
                .AnyAsync(d => d.DelegateMemberUserId == delegateMemberUserId
                               && d.StartAt < endAt
                               && d.EndAt > startAt, ct);

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

        public async Task AddAsync(CommitteeDelegation delegation, CancellationToken ct)
        {
            await _db.CommitteeDelegations.AddAsync(delegation, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<CommitteeDelegation?> GetByIdAsync(Guid committeeId, Guid delegationId, CancellationToken ct)
            => await _db.CommitteeDelegations
                .FirstOrDefaultAsync(d => d.Id == delegationId && d.InnovationCommitteeId == committeeId, ct);

        public async Task RemoveAndSaveAsync(CommitteeDelegation delegation, CancellationToken ct)
        {
            _db.CommitteeDelegations.Remove(delegation);
            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}