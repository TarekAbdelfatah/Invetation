namespace Ibtikar.ViewModels
{
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount)
    {
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    public sealed record TypedPagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount) : PagedResult<T>(Items, Page, PageSize, TotalCount);

    public static class PagedRequest
    {
        public const int DefaultPageSize = 20;

        public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
        {
            var p = page is null || page < 1 ? 1 : page.Value;
            var s = pageSize is null || pageSize < 1 ? DefaultPageSize : Math.Min(pageSize.Value, 100);
            return (p, s);
        }
    }
}
