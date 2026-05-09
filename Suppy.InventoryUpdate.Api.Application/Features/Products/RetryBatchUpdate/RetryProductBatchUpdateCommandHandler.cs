using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Errors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.RetryBatchUpdate;

internal sealed class RetryProductBatchUpdateCommandHandler
    : ICommandHandler<RetryProductBatchUpdateCommand, RetryProductBatchUpdateResult>
{
    private readonly IProductBatchProcessingStore _store;
    private readonly IClock _clock;

    public RetryProductBatchUpdateCommandHandler(
        IProductBatchProcessingStore store,
        IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Result<RetryProductBatchUpdateResult>> Handle(
        RetryProductBatchUpdateCommand request,
        CancellationToken ct = default)
    {
        if (request.BatchId == Guid.Empty)
        {
            return Result<RetryProductBatchUpdateResult>.Failure(
                ApplicationErrors.Validation("Batch id is required."));
        }

        var batch = await _store.GetBatchWithItemsAsync(request.BatchId, ct);
        if (batch is null)
        {
            return Result<RetryProductBatchUpdateResult>.Failure(
                ApplicationErrors.NotFound("Product update batch", request.BatchId.ToString()));
        }

        int retriedItems;
        try
        {
            retriedItems = batch.RetryFailedItems(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result<RetryProductBatchUpdateResult>.Failure(ApplicationErrors.Conflict(ex.Message));
        }

        return Result<RetryProductBatchUpdateResult>.Success(new RetryProductBatchUpdateResult(
            batch.Id,
            batch.Status.ToString(),
            batch.TotalItems,
            batch.ProcessedItems,
            batch.FailedItems,
            retriedItems));
    }
}
