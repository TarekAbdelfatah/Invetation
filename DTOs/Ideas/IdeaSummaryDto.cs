namespace Ibtikar.DTOs.Ideas
{
    public sealed record IdeaSummaryDto(
        Guid Id,
        string ReferenceNumber,
        string Title,
        string? StatusName,
        string? StatusColor,
        string? DomainName,
        string? DepartmentName,
        DateTime? SubmittedAt,
        DateTime CreatedAt);
}