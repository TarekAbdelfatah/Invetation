using Ibtikar.DTOs.Audit;

namespace Ibtikar.Services.Interfaces
{
    public interface IAuditService
    {
        Task<AuditInboxDto> GetInboxAsync(string? applicantType, string? status, CancellationToken ct);
        Task<IReadOnlyList<AuditInboxRowDto>> GetInboxRowsAsync(string? applicantType, string? status, CancellationToken ct);
        Task<AuditDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct);

        Task<AuditActionResultDto> OpenAsync(Guid id, Guid? auditorId, CancellationToken ct);
        Task<AuditActionResultDto> RouteAsync(Guid id, Guid departmentId, string? decisionText, Guid? auditorId, CancellationToken ct);
        Task<AuditActionResultDto> RejectAsync(Guid id, string? reason, Guid? auditorId, CancellationToken ct);
        Task<AuditActionResultDto> RequestCompletionAsync(Guid id, string? instructions, Guid? auditorId, CancellationToken ct);
    }
}