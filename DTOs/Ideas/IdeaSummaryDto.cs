namespace Ibtikar.DTOs.Ideas
{
    public sealed record IdeaSummaryDto(
        Guid Id,
        string ReferenceNumber,
        string Title,
        string TitleDisplay,
        string? StatusName,
        string? StatusColor,
        string? DomainName,
        string? DepartmentName,
        DateTime? SubmittedAt,
        DateTime CreatedAt);
}