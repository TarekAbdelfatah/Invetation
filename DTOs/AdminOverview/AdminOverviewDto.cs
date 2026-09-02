namespace Ibtikar.DTOs.AdminOverview
{
    public sealed record AdminOverviewDto(
        int TotalIdeas,
        int Drafts,
        int Submitted,
        int TotalUsers,
        IReadOnlyList<AdminOverviewStatusCountDto> ByStatus);

    public sealed record AdminOverviewStatusCountDto(string Code, string Name, string Color, int Count);

    public sealed record AdminOverviewIdeaRowDto(
        Guid Id,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string AssignedDepartmentName,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime CreatedAt,
        bool IsDraft);

    public sealed record AdminOverviewListDto(
        IReadOnlyList<AdminOverviewIdeaRowDto> Rows,
        string? StatusFilter,
        int TotalCount);

    public sealed record AdminOverviewAttachmentDto(
        Guid Id,
        string FileName,
        long SizeBytes,
        DateTime UploadedAt,
        string? UploadedByName);

    public sealed record AdminOverviewAssessmentLineDto(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int Score,
        string? Comment);

    public sealed record AdminOverviewAssessmentDto(
        Guid Id,
        string Source,
        string SourceLabel,
        string AssessorName,
        string DepartmentName,
        bool IsDraft,
        bool IsLocked,
        DateTime? SubmittedAt,
        decimal? TotalScore,
        string? Comment,
        IReadOnlyList<AdminOverviewAssessmentLineDto> Lines);

    public sealed record AdminOverviewTimelineRowDto(
        DateTime ChangedAt,
        string FromStatus,
        string ToStatus,
        string By,
        string? Note);

    public sealed record AdminOverviewDetailsDto(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string DomainName,
        string ExpectedImpactName,
        string TargetAudienceName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string? AssignedDepartmentName,
        string StatusName,
        string StatusColor,
        string StatusCode,
        bool IsDraft,
        DateTime? SubmittedAt,
        DateTime CreatedAt,
        IReadOnlyList<AdminOverviewAttachmentDto> Attachments,
        IReadOnlyList<AdminOverviewAssessmentDto> Assessments,
        IReadOnlyList<AdminOverviewTimelineRowDto> Timeline);
}