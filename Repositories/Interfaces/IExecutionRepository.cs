using Ibtikar.DTOs.Execution;

namespace Ibtikar.Repositories
{
    public interface IExecutionRepository
    {
        Task<ExecutionListDto> GetListAsync(Guid? departmentId, CancellationToken ct);
        Task<ExecutionHeaderDto?> GetHeaderAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<ExecutionTimelineDto?> GetTimelineAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<bool> IsAssigneeAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<Guid?> GetCompletedStatusIdAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
