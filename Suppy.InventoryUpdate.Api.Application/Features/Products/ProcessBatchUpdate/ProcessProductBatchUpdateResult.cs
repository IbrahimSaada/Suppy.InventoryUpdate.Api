namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ProcessBatchUpdate;

public sealed record ProcessProductBatchUpdateResult(
    Guid BatchId,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int FailedItems,
    bool WasClaimed);
