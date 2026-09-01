using Ibtikar.DTOs.Audit;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IAuditRepository
    {
        Task<AuditInboxDto> GetInboxRowsAsync(
            string applicantTypeFilter,
            IReadOnlyList<string> statusCodes,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<AuditDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct);

        Task<InnovationIdea?> GetForTransitionAsync(Guid id, CancellationToken ct);

        Task<Department?> GetActiveDepartmentAsync(Guid departmentId, CancellationToken ct);

        Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct);

        Task AddStatusHistoryAsync(IdeaStatusHistory history, CancellationToken ct);

        Task AddAuditActionAsync(AuditActionItem action, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}