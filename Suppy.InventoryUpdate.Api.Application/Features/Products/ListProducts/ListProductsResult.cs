namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ListProducts;

public sealed record ListProductsResult(
    IReadOnlyCollection<ProductSummaryResult> Items,
    long TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    int ReturnedCount,
    long RemainingCount);

public sealed record ProductSummaryResult(
    Guid Id,
    string TenantId,
    string ItemId,
    decimal Price,
    int Stock,
    string? MetadataJson,
    Guid LastBatchId,
    DateTime LastUpdatedFromBatchAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
