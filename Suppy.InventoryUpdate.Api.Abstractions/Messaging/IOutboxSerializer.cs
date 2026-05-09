namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IOutboxSerializer
{
    OutboxSerializedMessage Serialize(IIntegrationEvent integrationEvent);
}
