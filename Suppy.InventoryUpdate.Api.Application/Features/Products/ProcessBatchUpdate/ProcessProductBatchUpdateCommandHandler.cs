using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Errors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ProcessBatchUpdate;

internal sealed class ProcessProductBatchUpdateCommandHandler
    : ICommandHandler<ProcessProductBatchUpdateCommand, ProcessProductBatchUpdateResult>
{
    private readonly IProductBatchProcessingStore _store;
    private readonly IClock _clock;

    public ProcessProductBatchUpdateCommandHandler(
        IProductBatchProcessingStore store,
        IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Result<ProcessProductBatchUpdateResult>> Handle(
        ProcessProductBatchUpdateCommand request,
        CancellationToken ct = default)
    {
        if (request.BatchId == Guid.Empty)
        {
            return Result<ProcessProductBatchUpdateResult>.Failure(
                ApplicationErrors.Validation("Batch id is required."));
        }

        var utcNow = _clock.UtcNow;
        var wasClaimed = await _store.TryClaimAcceptedBatchAsync(request.BatchId, utcNow, ct);
        var batch = await _store.GetBatchWithItemsAsync(request.BatchId, ct);

        if (batch is null)
        {
            return Result<ProcessProductBatchUpdateResult>.Failure(
                ApplicationErrors.NotFound("Product update batch", request.BatchId.ToString()));
        }

        if (!wasClaimed)
        {
            return Result<ProcessProductBatchUpdateResult>.Success(ToResult(batch, wasClaimed: false));
        }

        var products = await _store.ListProductsByItemIdsAsync(
            batch.TenantId,
            batch.Items.Select(item => item.ItemId).ToArray(),
            ct);

        var productByItemId = products.ToDictionary(
            product => product.ItemId.Value,
            StringComparer.Ordinal);

        foreach (var item in batch.Items)
        {
            try
            {
                batch.MarkItemProcessing(item.Id, utcNow);

                if (productByItemId.TryGetValue(item.ItemId.Value, out var product))
                {
                    product.ApplyUpdate(item.Price, item.Stock, item.MetadataJson, batch.Id, utcNow);
                }
                else
                {
                    product = Product.Create(
                        batch.TenantId,
                        item.ItemId,
                        item.Price,
                        item.Stock,
                        item.MetadataJson,
                        batch.Id,
                        utcNow);

                    await _store.AddProductAsync(product, ct);
                    productByItemId.Add(product.ItemId.Value, product);
                }

                batch.MarkItemProcessed(item.Id, utcNow);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                batch.MarkItemFailed(item.Id, ex.Message, utcNow);
            }
        }

        return Result<ProcessProductBatchUpdateResult>.Success(ToResult(batch, wasClaimed: true));
    }

    private static ProcessProductBatchUpdateResult ToResult(
        ProductUpdateBatch batch,
        bool wasClaimed)
    {
        return new ProcessProductBatchUpdateResult(
            batch.Id,
            batch.Status.ToString(),
            batch.TotalItems,
            batch.ProcessedItems,
            batch.FailedItems,
            wasClaimed);
    }
}
