using Ibtikar.DTOs.Committee;

namespace Ibtikar.Services.Committee
{
    public interface ICommitteeDashboardService
    {
        Task<CommitteeDashboardDto> GetSnapshotAsync(Guid userId, CancellationToken ct);
        Task<CommitteeReferralsDto?> GetReferralsAsync(Guid userId, string statusFilter, CancellationToken ct);
        Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct);
    }
}
