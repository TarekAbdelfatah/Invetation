namespace Ibtikar.DTOs.AdminOverview
{
    public sealed record AdminOverviewDto(
        int TotalIdeas,
        int Drafts,
        int Submitted,
        int TotalUsers,
        IReadOnlyList<AdminOverviewStatusCountDto> ByStatus,
        IReadOnlyList<AdminOverviewRecentIdeaDto> Recent);

    public sealed record AdminOverviewStatusCountDto(string Code, string Name, string Color, int Count);

    public sealed record AdminOverviewRecentIdeaDto(
        string Reference,
        string Title,
        string StatusName,
        string StatusColor,
        string Domain,
        DateTime CreatedAt);

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
}