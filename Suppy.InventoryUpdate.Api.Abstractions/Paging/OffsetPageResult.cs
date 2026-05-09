namespace Suppy.InventoryUpdate.Api.Abstractions.Paging;

public sealed record OffsetPageResult<TItem>(
    IReadOnlyList<TItem> Items,
    long TotalCount,
    int Page,
    int PageSize)
{
    public long TotalPages => PageSize <= 0 ? 0 : (long)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public long SkippedCount => (Math.Max(Page, 1) - 1L) * Math.Max(PageSize, 1);
    public int ReturnedCount => Items.Count;

    public long RemainingCount
    {
        get
        {
            var remaining = TotalCount - (SkippedCount + ReturnedCount);
            return remaining > 0 ? remaining : 0;
        }
    }
}
