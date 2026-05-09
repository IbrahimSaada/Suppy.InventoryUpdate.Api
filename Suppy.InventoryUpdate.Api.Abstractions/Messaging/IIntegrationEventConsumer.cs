namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IIntegrationEventConsumer<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task ConsumeAsync(TIntegrationEvent integrationEvent, CancellationToken ct = default);
}
