using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Abstractions.Products;

public interface IProductReadStore
{
    Task<ProductPageReadResult> ListProductsAsync(
        TenantId tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

public sealed record ProductPageReadResult(
    IReadOnlyCollection<ProductReadRecord> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record ProductReadRecord(
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
