using Ibtikar.DTOs.Committee;

namespace Ibtikar.Services.Committee
{
    public interface ICommitteeDashboardService
    {
        Task<CommitteeDashboardDto> GetSnapshotAsync(Guid userId, CancellationToken ct);
        Task<CommitteeReferralsDto?> GetReferralsAsync(Guid userId, string statusFilter, CancellationToken ct);
        Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct);
        Task<CommitteeAssessDto?> GetAssessAsync(Guid userId, Guid ideaId, CancellationToken ct);
        Task<CommitteeAssessOutcomeDto> SaveAssessmentAsync(Guid userId, CommitteeAssessmentSubmissionDto submission, CancellationToken ct);
    }
}
