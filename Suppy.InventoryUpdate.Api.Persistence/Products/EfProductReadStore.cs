using Microsoft.EntityFrameworkCore;
using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;
using Suppy.InventoryUpdate.Api.Persistence.Sql;

namespace Suppy.InventoryUpdate.Api.Persistence.Products;

internal sealed class EfProductReadStore : IProductReadStore
{
    private readonly AppDbContext _dbContext;

    public EfProductReadStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductPageReadResult> ListProductsAsync(
        TenantId tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Max(pageSize, 1);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var source = _dbContext.Products
            .FromSqlRaw(
                """
                SELECT *
                FROM "Products"
                WHERE "TenantId" = {0}
                  AND "IsDeleted" = false
                """,
                tenantId.Value)
            .AsNoTracking();

        var totalCount = await source.LongCountAsync(ct);

        var products = await _dbContext.Products
            .FromSqlRaw(
                """
                SELECT *
                FROM "Products"
                WHERE "TenantId" = {0}
                  AND "IsDeleted" = false
                ORDER BY "ItemId", "Id"
                LIMIT {1} OFFSET {2}
                """,
                tenantId.Value,
                normalizedPageSize,
                skip)
            .AsNoTracking()
            .ToListAsync(ct);

        return new ProductPageReadResult(
            products
                .Select(product => new ProductReadRecord(
                    product.Id,
                    product.TenantId.Value,
                    product.ItemId.Value,
                    product.Price,
                    product.Stock,
                    product.MetadataJson,
                    product.LastBatchId,
                    product.LastUpdatedFromBatchAtUtc,
                    product.CreatedAtUtc,
                    product.UpdatedAtUtc))
                .ToArray(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }
}
