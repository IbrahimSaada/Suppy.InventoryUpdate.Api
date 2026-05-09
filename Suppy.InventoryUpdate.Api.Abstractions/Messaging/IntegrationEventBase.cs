namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public int Version { get; init; } = 1;

    public string? CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public string? IdempotencyKey { get; init; }

    public string? TenantId { get; init; }
}
