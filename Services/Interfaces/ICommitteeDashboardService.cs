using Ibtikar.DTOs.Committee;

namespace Ibtikar.Services.Interfaces
{
    public interface ICommitteeDashboardService
    {
        Task<CommitteeDashboardDto> GetSnapshotAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<CommitteeReferralRowDto>?> GetReferralsAsync(Guid userId, CancellationToken ct);
        Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct);
        Task<CommitteeAssessDto?> GetAssessAsync(Guid userId, Guid ideaId, CancellationToken ct);
        Task<CommitteeAssessOutcomeDto> SaveAssessmentAsync(Guid userId, CommitteeAssessmentSubmissionDto submission, CancellationToken ct);
        Task<CommitteeVotesDto?> GetVotesAsync(Guid userId, CancellationToken ct);
        Task<CommitteeVoteRowDto?> GetSingleVoteAsync(Guid userId, Guid ideaId, CancellationToken ct);
        Task<CommitteeVoteOutcomeDto> SubmitVoteAsync(Guid userId, CommitteeVoteSubmitDto submission, CancellationToken ct);
        Task<CommitteeDecisionDto?> GetDecisionAsync(Guid userId, Guid ideaId, CancellationToken ct);
        Task<CommitteeVoteOutcomeDto> AcceptAsync(Guid userId, Guid ideaId, bool extraConfirmed, CancellationToken ct);
        Task<CommitteeVoteOutcomeDto> RejectAsync(Guid userId, Guid ideaId, string reason, CancellationToken ct);
        Task<CommitteeVoteOutcomeDto> ReturnForDevelopmentAsync(Guid userId, Guid ideaId, CancellationToken ct);
    }
}
