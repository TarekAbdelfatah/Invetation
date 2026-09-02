namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeCriterionDto(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        int DisplayOrder);

    public sealed record CommitteeAssessLineDto(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int? Score,
        string? Comment);

    public sealed record CommitteeAssessDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusName,
        string StatusColor,
        bool IsDraftSaved,
        bool IsLocked,
        Guid? DraftHeaderId,
        DateTime? DraftSavedAt,
        decimal? TotalScore,
        string? Comment,
        IReadOnlyList<CommitteeCriterionDto> Criteria,
        IReadOnlyList<CommitteeAssessLineDto> Lines,
        int? DepartmentPercent,
        int? CommitteePercent,
        int? CombinedAverage);

    public sealed record CommitteeScoreInputDto(Guid CriterionId, int Score, string? Comment);

    public sealed record CommitteeAssessmentSubmissionDto(
        Guid IdeaId,
        Guid? HeaderId,
        IReadOnlyList<CommitteeScoreInputDto> Scores,
        string? Comment,
        bool SaveDraft);

    public sealed record CommitteeAssessOutcomeDto(bool Success, string Message, bool RequiresExtraConfirm);

    public sealed record CommitteeAcceptDto(
        Guid IdeaId,
        bool ExtraConfirmed);

    public sealed record CommitteeAssessIdeaDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusName,
        string StatusColor);

    public sealed record CommitteeDecisionIdeaDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusCode);

    public sealed record CommitteeDecisionDto(
        Guid IdeaId,
        string Reference,
        string Title,
        int? CombinedAverage,
        bool CanAccept,
        string? ExtraConfirmWarning);

    public sealed record CommitteeRejectDto(
        Guid IdeaId,
        string Reason);
}
