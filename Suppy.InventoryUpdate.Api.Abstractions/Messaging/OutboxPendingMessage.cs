namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public sealed record OutboxPendingMessage(
    Guid MessageId,
    string MessageType,
    string Payload,
    string? Headers,
    DateTime OccurredAtUtc,
    int RetryCount,
    string? CorrelationId,
    string? CausationId,
    string? IdempotencyKey,
    int Version);
