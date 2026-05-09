using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Errors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

internal sealed class SubmitProductBatchUpdateCommandHandler
    : ICommandHandler<SubmitProductBatchUpdateCommand, SubmitProductBatchUpdateResult>
{
    private readonly IRepository<ProductUpdateBatch, Guid> _batchRepository;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public SubmitProductBatchUpdateCommandHandler(
        IRepository<ProductUpdateBatch, Guid> batchRepository,
        IIntegrationEventPublisher integrationEventPublisher)
    {
        _batchRepository = batchRepository;
        _integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<Result<SubmitProductBatchUpdateResult>> Handle(
        SubmitProductBatchUpdateCommand request,
        CancellationToken ct = default)
    {
        TenantId tenantId;
        BatchIdempotencyKey? idempotencyKey;
        IReadOnlyList<ProductUpdateBatchItemDraft> itemDrafts;

        try
        {
            tenantId = TenantId.From(request.TenantId);
            idempotencyKey = BatchIdempotencyKey.FromOptional(request.IdempotencyKey);
            itemDrafts = request.Items
                .Select(item => new ProductUpdateBatchItemDraft(
                    ItemId.From(item.ItemId),
                    item.Price,
                    item.Stock,
                    item.MetadataJson))
                .ToArray();
        }
        catch (ArgumentException ex)
        {
            return Result<SubmitProductBatchUpdateResult>.Failure(ApplicationErrors.Validation(ex.Message));
        }

        if (idempotencyKey is not null)
        {
            var existingBatch = await _batchRepository.FirstOrDefaultAsync(
                batch =>
                    batch.TenantId.Value == tenantId.Value &&
                    batch.IdempotencyKey != null &&
                    batch.IdempotencyKey.Value == idempotencyKey.Value,
                includeDeleted: false,
                asNoTracking: true,
                includes: null,
                ct);

            if (existingBatch is not null)
            {
                return Result<SubmitProductBatchUpdateResult>.Success(ToResult(existingBatch, wasDuplicate: true));
            }
        }

        ProductUpdateBatch batch;
        try
        {
            batch = ProductUpdateBatch.Accept(tenantId, idempotencyKey, itemDrafts);
        }
        catch (ArgumentException ex)
        {
            return Result<SubmitProductBatchUpdateResult>.Failure(ApplicationErrors.Validation(ex.Message));
        }

        await _batchRepository.AddAsync(batch, ct: ct);

        await _integrationEventPublisher.PublishAsync(
            new ProductBatchUpdateAcceptedIntegrationEvent(batch.Id, batch.TotalItems)
            {
                TenantId = tenantId.Value,
                IdempotencyKey = idempotencyKey?.Value
            },
            ct);

        return Result<SubmitProductBatchUpdateResult>.Success(ToResult(batch, wasDuplicate: false));
    }

    private static SubmitProductBatchUpdateResult ToResult(
        ProductUpdateBatch batch,
        bool wasDuplicate)
    {
        return new SubmitProductBatchUpdateResult(
            batch.Id,
            batch.TenantId.Value,
            batch.Status.ToString(),
            batch.TotalItems,
            wasDuplicate);
    }
}
