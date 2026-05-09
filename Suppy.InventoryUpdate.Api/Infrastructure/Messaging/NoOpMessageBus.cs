using Suppy.InventoryUpdate.Api.Abstractions.Messaging;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class NoOpMessageBus : IMessageBus
{
    public Task PublishAsync(
        string? messageId,
        string messageType,
        string payload,
        string? headers = null,
        string? correlationId = null,
        string? causationId = null,
        string? idempotencyKey = null,
        int version = 1,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
