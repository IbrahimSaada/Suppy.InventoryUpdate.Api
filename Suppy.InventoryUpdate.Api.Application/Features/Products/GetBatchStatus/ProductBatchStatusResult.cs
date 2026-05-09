namespace Suppy.InventoryUpdate.Api.Application.Features.Products.GetBatchStatus;

public sealed record ProductBatchStatusResult(
    Guid BatchId,
    string TenantId,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int FailedItems,
    bool CanRetry,
    DateTime CreatedAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<ProductBatchItemStatusResult> Items);

public sealed record ProductBatchItemStatusResult(
    Guid Id,
    string ItemId,
    string Status,
    string? ErrorMessage);
