using Ibtikar.Models;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Repositories
{
    public interface IDelegationRepository
    {
        Task<bool> HasOverlapAsync(Guid committeeId, Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct);
        Task<bool> HasCommitteeOverlapAsync(Guid committeeId, DateTime startAt, DateTime endAt, CancellationToken ct);
        Task<CommitteeDelegation?> GetActiveAsync(Guid committeeId, CancellationToken ct);
        Task<bool> IsDelegateAsync(Guid userId, CancellationToken ct);
        Task<bool> HasActiveDelegationAsync(Guid delegateMemberUserId, DateTime startAt, DateTime endAt, CancellationToken ct);
        Task<IReadOnlyList<DelegationRowDto>> GetDelegationsAsync(Guid committeeId, CancellationToken ct);
        Task AddAsync(CommitteeDelegation delegation, CancellationToken ct);
        Task<CommitteeDelegation?> GetByIdAsync(Guid committeeId, Guid delegationId, CancellationToken ct);
        Task RemoveAndSaveAsync(CommitteeDelegation delegation, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}