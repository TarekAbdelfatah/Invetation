namespace Ibtikar.DTOs.Reports
{
    public sealed record ReportsDateRangeDto(
        DateTime From,
        DateTime To);

    public sealed record ReportsKpiDto(
        int TotalIdeas,
        int Submitted,
        int Approved,
        int InExecution,
        int Completed);

    public sealed record ReportsStageMixRowDto(
        string Code,
        string Name,
        string Color,
        int Count,
        double Percent);

    public sealed record ReportsSummaryDto(
        ReportsKpiDto Kpis,
        IReadOnlyList<ReportsStageMixRowDto> StageMix,
        ReportsDateRangeDto Range,
        bool IsEmpty,
        string? Warning);

    public sealed record ReportsChallengeRowDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string DomainName,
        string ApplicantName,
        string ApplicantDepartmentName,
        string ProblemStatement,
        string? ProposedSolution,
        DateTime CreatedAt,
        string StatusName,
        string StatusColor);

    public sealed record ReportsChallengesDto(
        IReadOnlyList<ReportsChallengeRowDto> Rows,
        IReadOnlyList<ReportsDomainOptionDto> Domains,
        Guid? DomainId,
        ReportsDateRangeDto? Range,
        int TotalCount);
}
