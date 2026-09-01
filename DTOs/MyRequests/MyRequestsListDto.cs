namespace Ibtikar.DTOs.MyRequests
{
    public sealed record MyRequestSummaryDto(
        Guid Id,
        string Reference,
        string Title,
        string? DomainName,
        bool IsDraft,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime CreatedAt,
        DateTime? SubmittedAt);

    public sealed record MyRequestsListDto(
        IReadOnlyList<MyRequestSummaryDto> Items,
        int Page,
        int PageSize,
        int TotalCount);
}
