using System.Text.Json;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class SystemTextJsonOutboxSerializer : IOutboxSerializer
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions HeaderJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OutboxSerializedMessage Serialize(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var messageType = $"{eventType.FullName}, {eventType.Assembly.GetName().Name}";
        var payload = JsonSerializer.Serialize(integrationEvent, eventType, PayloadJsonOptions);

        string? headers = null;
        if (!string.IsNullOrWhiteSpace(integrationEvent.TenantId))
        {
            headers = JsonSerializer.Serialize(
                new Dictionary<string, string?>
                {
                    ["tenantId"] = integrationEvent.TenantId
                },
                HeaderJsonOptions);
        }

        return new OutboxSerializedMessage(
            MessageId: integrationEvent.EventId,
            MessageType: messageType,
            Payload: payload,
            Headers: headers,
            OccurredAtUtc: integrationEvent.OccurredAtUtc,
            CorrelationId: integrationEvent.CorrelationId,
            CausationId: integrationEvent.CausationId,
            IdempotencyKey: integrationEvent.IdempotencyKey,
            Version: integrationEvent.Version);
    }
}
