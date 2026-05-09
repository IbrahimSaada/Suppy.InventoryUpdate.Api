namespace Suppy.InventoryUpdate.Api.Abstractions.Paging;

public sealed record CursorPageRequest(
    int Limit = 50,
    string? Cursor = null);
