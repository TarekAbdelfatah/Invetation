using Ibtikar.DTOs.PartnerDashboard;

namespace Ibtikar.Services.PartnerDashboard
{
    public interface IPartnerDashboardService
    {
        Task<PartnerDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct);
        Task<PartnerInboxDto?> GetInboxAsync(Guid? departmentId, CancellationToken ct);
        Task<PartnerDetailsDto?> GetDetailsAsync(Guid? departmentId, Guid assignmentId, CancellationToken ct);
        Task<PartnerSubmitOutcomeDto> SubmitAsync(
            Guid? departmentId,
            Guid actorUserId,
            PartnerSubmitDto submission,
            CancellationToken ct);
        Task<PartnerSubmitOutcomeDto> ReturnNotCompetentAsync(
            Guid? departmentId,
            Guid actorUserId,
            Guid assignmentId,
            string reason,
            CancellationToken ct);
        bool IsReturnNotCompetentAllowed(DateTime sentAtUtc, DateTime nowUtc);
    }
}