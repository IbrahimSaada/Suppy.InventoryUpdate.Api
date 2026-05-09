using Suppy.InventoryUpdate.Api.Abstractions.Messaging;

namespace Suppy.InventoryUpdate.Api.Persistence.Outbox;

internal sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string? Headers { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime AvailableAtUtc { get; set; }

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public string? LockId { get; set; }

    public DateTime? LockedAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public string? IdempotencyKey { get; set; }

    public int Version { get; set; }
}
