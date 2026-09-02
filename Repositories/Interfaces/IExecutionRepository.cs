using Ibtikar.DTOs.Execution;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IExecutionRepository
    {
        Task<ExecutionListDto> GetListAsync(Guid? departmentId, CancellationToken ct);
        Task<ExecutionHeaderDto?> GetHeaderAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<ExecutionTimelineDto?> GetTimelineAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<bool> IsAssigneeAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<Guid?> GetCompletedStatusIdAsync(CancellationToken ct);
        Task<InnovationIdea?> GetIdeaWithStatusAsync(Guid ideaId, CancellationToken ct);
        Task<ExecutionStage?> GetActiveStageByIdAsync(Guid stageId, CancellationToken ct);
        Task AddProgressAsync(ExecutionProgress progress, CancellationToken ct);
        Task AddProgressAndStatusAsync(ExecutionProgress progress, IdeaStatusHistory history, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
