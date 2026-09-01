using Ibtikar.DTOs.Execution;

namespace Ibtikar.Services.Interfaces
{
    public interface IExecutionService
    {
        Task<ExecutionListDto> GetListAsync(Guid? departmentId, CancellationToken ct);
        Task<ExecutionHeaderDto?> GetHeaderAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<ExecutionTimelineDto?> GetTimelineAsync(Guid ideaId, Guid? departmentId, CancellationToken ct);
        Task<ExecutionActionOutcomeDto> UpdateStageAsync(ExecutionUpdateDto dto, Guid? userId, Guid? departmentId, CancellationToken ct);
        Task<ExecutionActionOutcomeDto> CompleteAsync(ExecutionCompleteDto dto, Guid? userId, Guid? departmentId, CancellationToken ct);
    }
}
