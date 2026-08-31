namespace Ibtikar.DTOs.PartnerDashboard
{
    public sealed record PartnerDashboardDto(
        int PendingAssignments,
        int OverdueLate,
        int SubmittedThisCycle);

    public sealed record PartnerAssignmentRowDto(
        Guid AssignmentId,
        Guid IdeaId,
        string IdeaReference,
        string IdeaTitle,
        string ApplicantName,
        string SourceDepartmentName,
        DateTime SentAt,
        DateTime? RespondedAt,
        string Status,
        bool IsLate,
        bool IsPending,
        bool IsReturned,
        double DaysOpen);

    public sealed record PartnerInboxDto(
        IReadOnlyList<PartnerAssignmentRowDto> Items,
        int Total);

    public sealed record PartnerDetailsDto(
        Guid AssignmentId,
        Guid IdeaId,
        string IdeaReference,
        string IdeaTitle,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? DomainName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string SourceDepartmentName,
        string Status,
        DateTime SentAt,
        DateTime? RespondedAt,
        bool CanScore,
        bool AlreadyScored,
        IReadOnlyList<PartnerCriterionDto> Criteria,
        IReadOnlyList<PartnerScoreLineDto> ExistingScores,
        decimal? TotalScore,
        string? Comment);

    public sealed record PartnerCriterionDto(
        Guid Id,
        string Code,
        string Name,
        int DisplayOrder);

    public sealed record PartnerScoreLineDto(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int? Score,
        string? Comment);

    public sealed record PartnerSubmitDto(
        Guid AssignmentId,
        IReadOnlyList<PartnerScoreInputDto> Scores,
        string? Comment,
        bool ReturnOnly);

    public sealed record PartnerScoreInputDto(Guid CriterionId, int Score, string? Comment);

    public sealed record PartnerSubmitOutcomeDto(
        bool Success,
        string? Message,
        decimal? TotalScore);
}