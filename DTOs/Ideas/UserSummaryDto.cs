namespace Ibtikar.DTOs.Ideas
{
    public sealed record UserSummaryDto(
        Guid Id,
        string FullName,
        string? DepartmentName);
}