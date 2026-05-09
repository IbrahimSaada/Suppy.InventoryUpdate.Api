using Suppy.InventoryUpdate.Api.Abstractions.Messaging;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class NoOpOutboxStore : IOutboxStore
{
    public Task EnqueueAsync(OutboxMessageToEnqueue message, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task EnqueueManyAsync(IEnumerable<OutboxMessageToEnqueue> messages, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxPendingMessage>> ClaimBatchAsync(
        int batchSize,
        string lockId,
        DateTime utcNow,
        TimeSpan lockTimeout,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<OutboxPendingMessage>>(Array.Empty<OutboxPendingMessage>());
    }

    public Task MarkSucceededAsync(
        Guid messageId,
        string lockId,
        DateTime processedAtUtc,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(
        Guid messageId,
        string lockId,
        string error,
        DateTime nextAvailableAtUtc,
        bool moveToPoison,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
