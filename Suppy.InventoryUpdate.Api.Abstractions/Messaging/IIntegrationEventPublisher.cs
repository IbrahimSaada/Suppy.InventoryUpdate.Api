namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct = default);

    Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken ct = default);
}
