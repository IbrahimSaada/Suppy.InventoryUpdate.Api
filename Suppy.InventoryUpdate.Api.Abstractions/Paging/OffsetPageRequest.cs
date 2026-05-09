namespace Suppy.InventoryUpdate.Api.Abstractions.Paging;

public sealed record OffsetPageRequest(
    int Page = 1,
    int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Math.Max(PageSize, 1);
}
