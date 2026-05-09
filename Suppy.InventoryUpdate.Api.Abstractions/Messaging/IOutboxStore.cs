namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IOutboxStore
{
    Task EnqueueAsync(OutboxMessageToEnqueue message, CancellationToken ct = default);

    Task EnqueueManyAsync(IEnumerable<OutboxMessageToEnqueue> messages, CancellationToken ct = default);

    Task<IReadOnlyList<OutboxPendingMessage>> ClaimBatchAsync(
        int batchSize,
        string lockId,
        DateTime utcNow,
        TimeSpan lockTimeout,
        CancellationToken ct = default);

    Task MarkSucceededAsync(
        Guid messageId,
        string lockId,
        DateTime processedAtUtc,
        CancellationToken ct = default);

    Task MarkFailedAsync(
        Guid messageId,
        string lockId,
        string error,
        DateTime nextAvailableAtUtc,
        bool moveToPoison,
        CancellationToken ct = default);
}
