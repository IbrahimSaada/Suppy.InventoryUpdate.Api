using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Errors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ListProducts;

internal sealed class ListProductsQueryHandler : IQueryHandler<ListProductsQuery, ListProductsResult>
{
    private readonly IProductReadStore _productReadStore;

    public ListProductsQueryHandler(IProductReadStore productReadStore)
    {
        _productReadStore = productReadStore;
    }

    public async Task<Result<ListProductsResult>> Handle(
        ListProductsQuery request,
        CancellationToken ct = default)
    {
        TenantId tenantId;

        try
        {
            tenantId = TenantId.From(request.TenantId);
        }
        catch (ArgumentException ex)
        {
            return Result<ListProductsResult>.Failure(ApplicationErrors.Validation(ex.Message));
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, ListProductsQueryValidator.MaxPageSize);
        var result = await _productReadStore.ListProductsAsync(tenantId, page, pageSize, ct);

        var skip = (long)(result.Page - 1) * result.PageSize;
        var returnedCount = result.Items.Count;
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

        var remainingCount = Math.Max(0, result.TotalCount - skip - returnedCount);

        return Result<ListProductsResult>.Success(new ListProductsResult(
            result.Items
                .Select(item => new ProductSummaryResult(
                    item.Id,
                    item.TenantId,
                    item.ItemId,
                    item.Price,
                    item.Stock,
                    item.MetadataJson,
                    item.LastBatchId,
                    item.LastUpdatedFromBatchAtUtc,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc))
                .ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize,
            totalPages,
            result.Page > 1,
            result.Page < totalPages,
            returnedCount,
            remainingCount));
    }
}
