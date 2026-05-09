using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Application.Dispatching;
using Suppy.InventoryUpdate.Api.Application.Features.Products.ProcessBatchUpdate;
using Microsoft.Extensions.Logging;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

internal sealed class ProductBatchUpdateAcceptedIntegrationEventConsumer
    : IIntegrationEventConsumer<ProductBatchUpdateAcceptedIntegrationEvent>
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly ILogger<ProductBatchUpdateAcceptedIntegrationEventConsumer> _logger;

    public ProductBatchUpdateAcceptedIntegrationEventConsumer(
        IRequestDispatcher dispatcher,
        ILogger<ProductBatchUpdateAcceptedIntegrationEventConsumer> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task ConsumeAsync(
        ProductBatchUpdateAcceptedIntegrationEvent integrationEvent,
        CancellationToken ct = default)
    {
        var result = await _dispatcher.Send(
            new ProcessProductBatchUpdateCommand(integrationEvent.BatchId),
            ct);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Product batch '{integrationEvent.BatchId}' processing failed from RabbitMQ consumer: " +
                $"{result.Error.Code} {result.Error.Message}");
        }

        _logger.LogInformation(
            "Product batch {BatchId} processed from RabbitMQ event. Status={Status}, Processed={ProcessedItems}, Failed={FailedItems}.",
            result.Value.BatchId,
            result.Value.Status,
            result.Value.ProcessedItems,
            result.Value.FailedItems);
    }
}
