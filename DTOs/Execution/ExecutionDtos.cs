namespace Ibtikar.DTOs.Execution
{
    public sealed record ExecutionListItemDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        DateTime? AssignedAt,
        string? CurrentStageName,
        string StatusName,
        string StatusColor,
        bool CanUpdate,
        bool CanComplete);

    public sealed record ExecutionListDto(
        IReadOnlyList<ExecutionListItemDto> Items,
        string DepartmentName);

    public sealed record ExecutionHeaderDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string AssignedDepartmentName,
        string StatusName,
        string StatusColor,
        IReadOnlyList<ExecutionStageDto> Stages,
        ExecutionStageDto? CurrentStage,
        bool CanUpdate,
        bool CanComplete);

    public sealed record ExecutionStageDto(
        Guid Id,
        int Order,
        string Code,
        string Name);

    public sealed record ExecutionUpdateDto(
        Guid IdeaId,
        Guid ExecutionStageId,
        string Note);

    public sealed record ExecutionCompleteDto(
        Guid IdeaId,
        Guid CompletionStageId,
        string Note,
        IReadOnlyList<Guid> AttachmentIds);

    public sealed record ExecutionTimelineRowDto(
        DateTime ChangedAt,
        string StageName,
        int StageOrder,
        string? ChangedByName,
        string? Note);

    public sealed record ExecutionTimelineDto(
        Guid IdeaId,
        string Reference,
        string Title,
        IReadOnlyList<ExecutionTimelineRowDto> Rows);

    public sealed record ExecutionActionOutcomeDto(
        bool Success,
        string? Message,
        bool RequiresConfirmation = false);

    public sealed record ExecutionConfirmDto(
        Guid IdeaId,
        bool Confirmed);
}
