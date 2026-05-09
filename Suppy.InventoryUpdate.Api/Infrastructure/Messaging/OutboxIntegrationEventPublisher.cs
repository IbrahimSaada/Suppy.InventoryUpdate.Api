using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Abstractions.Observability;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxSerializer _outboxSerializer;
    private readonly IClock _clock;
    private readonly ICorrelationContext _correlationContext;

    public OutboxIntegrationEventPublisher(
        IOutboxStore outboxStore,
        IOutboxSerializer outboxSerializer,
        IClock clock,
        ICorrelationContext correlationContext)
    {
        _outboxStore = outboxStore;
        _outboxSerializer = outboxSerializer;
        _clock = clock;
        _correlationContext = correlationContext;
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var serialized = _outboxSerializer.Serialize(integrationEvent);
        var correlationId = ResolveCorrelationId(serialized.CorrelationId);
        var causationId = ResolveCausationId(serialized.CausationId, correlationId);

        var message = new OutboxMessageToEnqueue(
            MessageId: serialized.MessageId,
            MessageType: serialized.MessageType,
            Payload: serialized.Payload,
            Headers: serialized.Headers,
            OccurredAtUtc: serialized.OccurredAtUtc,
            AvailableAtUtc: _clock.UtcNow,
            CorrelationId: correlationId,
            CausationId: causationId,
            IdempotencyKey: serialized.IdempotencyKey,
            Version: serialized.Version);

        await _outboxStore.EnqueueAsync(message, ct);
    }

    public async Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvents);

        var now = _clock.UtcNow;
        var messages = integrationEvents
            .Where(x => x is not null)
            .Select(integrationEvent =>
            {
                var serialized = _outboxSerializer.Serialize(integrationEvent);
                var correlationId = ResolveCorrelationId(serialized.CorrelationId);
                var causationId = ResolveCausationId(serialized.CausationId, correlationId);

                return new OutboxMessageToEnqueue(
                    MessageId: serialized.MessageId,
                    MessageType: serialized.MessageType,
                    Payload: serialized.Payload,
                    Headers: serialized.Headers,
                    OccurredAtUtc: serialized.OccurredAtUtc,
                    AvailableAtUtc: now,
                    CorrelationId: correlationId,
                    CausationId: causationId,
                    IdempotencyKey: serialized.IdempotencyKey,
                    Version: serialized.Version);
            })
            .ToArray();

        if (messages.Length == 0)
        {
            return;
        }

        await _outboxStore.EnqueueManyAsync(messages, ct);
    }

    private string? ResolveCorrelationId(string? explicitCorrelationId)
    {
        if (!string.IsNullOrWhiteSpace(explicitCorrelationId))
        {
            return explicitCorrelationId;
        }

        return _correlationContext.CorrelationId;
    }

    private static string? ResolveCausationId(string? explicitCausationId, string? fallbackCorrelationId)
    {
        if (!string.IsNullOrWhiteSpace(explicitCausationId))
        {
            return explicitCausationId;
        }

        return fallbackCorrelationId;
    }
}
