namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IMessageBus
{
    Task PublishAsync(
        string? messageId,
        string messageType,
        string payload,
        string? headers = null,
        string? correlationId = null,
        string? causationId = null,
        string? idempotencyKey = null,
        int version = 1,
        CancellationToken ct = default);
}
