namespace Suppy.InventoryUpdate.Api.Application.Features.Products.RetryBatchUpdate;

public sealed record RetryProductBatchUpdateResult(
    Guid BatchId,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int FailedItems,
    int RetriedItems);
