namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredAtUtc { get; }

    int Version { get; }

    string? CorrelationId { get; }

    string? CausationId { get; }

    string? IdempotencyKey { get; }

    string? TenantId { get; }
}
