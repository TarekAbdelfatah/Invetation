using Ibtikar.DTOs.Committee;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface ICommitteeDashboardRepository
    {
        Task<CommitteeDashboardDto> GetSnapshotCountsAsync(CancellationToken ct);
        Task<CommitteeReferralListDto> GetReferralsAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task<CommitteeAssessIdeaDto?> GetAssessIdeaAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyList<CommitteeCriterionDto>> GetActiveCriteriaAsync(CancellationToken ct);
        Task<int> CountActiveCriteriaAsync(CancellationToken ct);
        Task<AssessmentHeader?> GetLatestCommitteeHeaderAsync(Guid ideaId, Guid userId, CancellationToken ct);
        Task<AssessmentHeader?> GetCommitteeHeaderForSaveAsync(Guid ideaId, Guid userId, Guid? headerId, CancellationToken ct);
        Task<AssessmentHeader?> GetLatestSubmittedHeaderAsync(Guid ideaId, string source, CancellationToken ct);
        void AddAssessmentHeader(AssessmentHeader header);
        void RemoveAssessmentDetails(IEnumerable<AssessmentDetail> details);
        void AddAssessmentDetail(AssessmentDetail detail);
        Task<bool> IdeaExistsAsync(Guid ideaId, CancellationToken ct);
        Task<bool> HasSubmittedAssessmentAsync(Guid ideaId, Guid userId, CancellationToken ct);
        Task<Guid?> GetIdeaCurrentStatusIdAsync(Guid ideaId, CancellationToken ct);
        Task<string?> GetStatusCodeByIdAsync(Guid statusId, CancellationToken ct);
        Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct);
        Task<InnovationIdea?> GetIdeaWithStatusAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyList<CommitteeVoteIdeaDto>> GetVoteIdeasAsync(CancellationToken ct);
        Task<CommitteeIdeaReadOnlyDto?> GetIdeaReadOnlyAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyDictionary<Guid, string>> GetVotesByUserAsync(Guid userId, IReadOnlyCollection<Guid> ideaIds, CancellationToken ct);
        Task<bool> HasVotedAsync(Guid ideaId, Guid userId, CancellationToken ct);
        Task AddVoteAsync(CommitteeVote vote, CancellationToken ct);
        Task<CommitteeDecisionIdeaDto?> GetDecisionIdeaAsync(Guid ideaId, CancellationToken ct);
        Task AddStatusHistoryAndSaveAsync(IdeaStatusHistory history, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}