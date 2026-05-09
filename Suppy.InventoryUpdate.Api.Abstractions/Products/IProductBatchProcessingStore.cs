using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Abstractions.Products;

public interface IProductBatchProcessingStore
{
    Task<IReadOnlyList<Guid>> ListAcceptedBatchIdsAsync(int take, CancellationToken ct = default);

    Task<bool> TryClaimAcceptedBatchAsync(Guid batchId, DateTime utcNow, CancellationToken ct = default);

    Task<ProductUpdateBatch?> GetBatchByIdempotencyKeyAsync(
        TenantId tenantId,
        BatchIdempotencyKey idempotencyKey,
        CancellationToken ct = default);

    Task<ProductUpdateBatch?> GetBatchWithItemsAsync(Guid batchId, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> ListProductsByItemIdsAsync(
        TenantId tenantId,
        IReadOnlyCollection<ItemId> itemIds,
        CancellationToken ct = default);

    Task AddProductAsync(Product product, CancellationToken ct = default);
}
