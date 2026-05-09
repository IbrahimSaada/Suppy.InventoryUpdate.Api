namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string ExchangeName { get; set; } = "backend.foundation.events";

    public string ExchangeType { get; set; } = "topic";

    public string DefaultRoutingKey { get; set; } = "integration.event";

    public string ClientProvidedName { get; set; } = "suppy-inventory-update";

    public bool AutomaticRecoveryEnabled { get; set; } = true;

    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    public bool PublisherConfirmsEnabled { get; set; } = true;

    public int PublisherConfirmTimeoutSeconds { get; set; } = 10;

    public bool ConsumerEnabled { get; set; }

    public string ConsumerQueueName { get; set; } = "backend.foundation.integration";

    public string[] ConsumerRoutingKeys { get; set; } = ["#"];

    public ushort ConsumerPrefetchCount { get; set; } = 1;

    public int ConsumerIdempotencyTtlHours { get; set; } = 24;

    public string DeadLetterExchangeName { get; set; } = "backend.foundation.events.dlx";

    public string DeadLetterQueueName { get; set; } = "backend.foundation.integration.dlq";

    public string DeadLetterRoutingKey { get; set; } = "dead.letter";
}
