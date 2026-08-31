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
}