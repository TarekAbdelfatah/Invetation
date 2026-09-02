using Ibtikar.DTOs.Committees;
using Ibtikar.Models;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Repositories
{
    public interface ICommitteeRepository
    {
        Task<IReadOnlyList<CommitteeSummaryDto>> GetAllAsync(CancellationToken ct);
        Task<CommitteeMemberOptionDto[]> GetMemberCandidatesAsync(Guid? excludeCommitteeId, CancellationToken ct);
        Task AddCommitteeAsync(InnovationCommittee committee, CancellationToken ct);
        Task<InnovationCommittee?> GetWithMembersAsync(Guid committeeId, CancellationToken ct);
        Task<bool> IsHeadAsync(Guid committeeId, Guid userId, CancellationToken ct);
        Task<bool> IsMemberAsync(Guid committeeId, Guid userId, CancellationToken ct);
        Task<bool> IsActiveMemberAsync(Guid userId, CancellationToken ct);
        Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct);
        Task<Guid?> GetCommitteeIdForHeadAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<DelegationMemberOptionDto>> GetDelegateCandidatesAsync(Guid committeeId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}