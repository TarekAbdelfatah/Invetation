using Ibtikar.DTOs.SpecializedDashboard;

namespace Ibtikar.Services.Interfaces
{
    public interface ISpecializedDashboardService
    {
        Task<SpecializedDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct);

        Task<SpecializedReferralsDto?> GetReferralsAsync(Guid? departmentId, string? status, int page, int pageSize, CancellationToken ct);

        Task<SpecializedDetailsDto?> GetDetailsAsync(Guid? departmentId, Guid ideaId, CancellationToken ct);

        Task<SpecializedAssessVmDto?> GetAssessVmAsync(Guid? departmentId, Guid ideaId, CancellationToken ct);

        Task<SpecializedAssessmentOutcomeDto> SaveAssessmentAsync(
            Guid? departmentId,
            Guid actorUserId,
            SpecializedAssessmentSubmissionDto submission,
            CancellationToken ct);

        Task<SpecializedRequestDto?> GetRequestVmAsync(Guid? departmentId, Guid ideaId, CancellationToken ct);

        Task<SpecializedRequestOutcomeDto> RequestPartnerOpinionsAsync(
            Guid? departmentId,
            Guid actorUserId,
            SpecializedRequestSubmissionDto submission,
            CancellationToken ct);

        Task<SpecializedPartnerOpinionDto?> GetPartnerOpinionAsync(Guid? departmentId, Guid ideaId, CancellationToken ct);

        Task<SpecializedSendToCommitteeDto?> GetSendToCommitteeSummaryAsync(Guid? departmentId, Guid ideaId, CancellationToken ct);

        Task<SpecializedSendToCommitteeOutcomeDto> SendToCommitteeAsync(
            Guid? departmentId,
            Guid actorUserId,
            Guid ideaId,
            bool skipPartnerWarning,
            CancellationToken ct);
    }
}