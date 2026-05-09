using Microsoft.EntityFrameworkCore;
using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;
using Suppy.InventoryUpdate.Api.Persistence.Sql;

namespace Suppy.InventoryUpdate.Api.Persistence.Products;

internal sealed class EfProductBatchProcessingStore : IProductBatchProcessingStore
{
    private readonly AppDbContext _dbContext;

    public EfProductBatchProcessingStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> ListAcceptedBatchIdsAsync(
        int take,
        CancellationToken ct = default)
    {
        if (take <= 0)
        {
            return Array.Empty<Guid>();
        }

        return await _dbContext.ProductUpdateBatches
            .AsNoTracking()
            .Where(batch => batch.Status == ProductUpdateBatchStatus.Accepted)
            .OrderBy(batch => batch.CreatedAtUtc)
            .ThenBy(batch => batch.Id)
            .Take(take)
            .Select(batch => batch.Id)
            .ToListAsync(ct);
    }

    public async Task<bool> TryClaimAcceptedBatchAsync(
        Guid batchId,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        if (batchId == Guid.Empty)
        {
            return false;
        }

        var affectedRows = await _dbContext.ProductUpdateBatches
            .Where(batch =>
                batch.Id == batchId &&
                batch.Status == ProductUpdateBatchStatus.Accepted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.Status, ProductUpdateBatchStatus.Processing)
                .SetProperty(batch => batch.ProcessingStartedAtUtc, utcNow)
                .SetProperty(batch => batch.UpdatedAtUtc, utcNow), ct);

        return affectedRows == 1;
    }

    public Task<ProductUpdateBatch?> GetBatchByIdempotencyKeyAsync(
        TenantId tenantId,
        BatchIdempotencyKey idempotencyKey,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(idempotencyKey);

        return _dbContext.ProductUpdateBatches
            .FromSqlRaw(
                """
                SELECT *
                FROM "ProductUpdateBatches"
                WHERE "TenantId" = {0}
                  AND "IdempotencyKey" = {1}
                  AND "IsDeleted" = false
                """,
                tenantId.Value,
                idempotencyKey.Value)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public Task<ProductUpdateBatch?> GetBatchWithItemsAsync(
        Guid batchId,
        CancellationToken ct = default)
    {
        return _dbContext.ProductUpdateBatches
            .Include(batch => batch.Items)
            .FirstOrDefaultAsync(batch => batch.Id == batchId, ct);
    }

    public async Task<IReadOnlyList<Product>> ListProductsByItemIdsAsync(
        TenantId tenantId,
        IReadOnlyCollection<ItemId> itemIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(itemIds);

        var itemIdValues = itemIds
            .Select(itemId => itemId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (itemIdValues.Length == 0)
        {
            return Array.Empty<Product>();
        }

        return await _dbContext.Products
            .FromSqlRaw(
                """
                SELECT *
                FROM "Products"
                WHERE "TenantId" = {0}
                  AND "ItemId" = ANY({1})
                  AND "IsDeleted" = false
                """,
                tenantId.Value,
                itemIdValues)
            .ToListAsync(ct);
    }

    public async Task AddProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        await _dbContext.Products.AddAsync(product, ct);
    }
}
