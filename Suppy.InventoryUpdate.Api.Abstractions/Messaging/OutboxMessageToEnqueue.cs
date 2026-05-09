namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public sealed record OutboxMessageToEnqueue(
    Guid MessageId,
    string MessageType,
    string Payload,
    string? Headers,
    DateTime OccurredAtUtc,
    DateTime AvailableAtUtc,
    string? CorrelationId,
    string? CausationId,
    string? IdempotencyKey,
    int Version);
