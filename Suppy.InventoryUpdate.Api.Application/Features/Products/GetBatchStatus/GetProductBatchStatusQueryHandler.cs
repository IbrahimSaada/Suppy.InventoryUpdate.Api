using System.Linq.Expressions;
using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Errors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.GetBatchStatus;

internal sealed class GetProductBatchStatusQueryHandler
    : IQueryHandler<GetProductBatchStatusQuery, ProductBatchStatusResult>
{
    private static readonly IReadOnlyList<Expression<Func<ProductUpdateBatch, object>>> Includes =
        new Expression<Func<ProductUpdateBatch, object>>[]
        {
            batch => batch.Items
        };

    private readonly IRepository<ProductUpdateBatch, Guid> _batchRepository;

    public GetProductBatchStatusQueryHandler(IRepository<ProductUpdateBatch, Guid> batchRepository)
    {
        _batchRepository = batchRepository;
    }

    public async Task<Result<ProductBatchStatusResult>> Handle(
        GetProductBatchStatusQuery request,
        CancellationToken ct = default)
    {
        if (request.BatchId == Guid.Empty)
        {
            return Result<ProductBatchStatusResult>.Failure(
                ApplicationErrors.Validation("Batch id is required."));
        }

        var batch = await _batchRepository.FirstOrDefaultAsync(
            item => item.Id == request.BatchId,
            includeDeleted: false,
            asNoTracking: true,
            includes: Includes,
            ct);

        if (batch is null)
        {
            return Result<ProductBatchStatusResult>.Failure(
                ApplicationErrors.NotFound("Product update batch", request.BatchId.ToString()));
        }

        return Result<ProductBatchStatusResult>.Success(new ProductBatchStatusResult(
            batch.Id,
            batch.TenantId.Value,
            batch.Status.ToString(),
            batch.TotalItems,
            batch.ProcessedItems,
            batch.FailedItems,
            batch.FailedItems > 0 &&
            batch.Status is ProductUpdateBatchStatus.Failed or ProductUpdateBatchStatus.PartiallyFailed,
            batch.CreatedAtUtc,
            batch.ProcessingStartedAtUtc,
            batch.CompletedAtUtc,
            batch.Items
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.Id)
                .Select(item => new ProductBatchItemStatusResult(
                    item.Id,
                    item.ItemId.Value,
                    item.Status.ToString(),
                    item.ErrorMessage))
                .ToArray()));
    }
}
