namespace Ibtikar.ViewModels
{
    public sealed record IdeaListItemVm(
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