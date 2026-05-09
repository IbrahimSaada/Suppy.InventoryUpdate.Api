using System.Text;
using System.Text.RegularExpressions;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private static readonly Regex InvalidRoutingKeyCharacters =
        new("[^a-zA-Z0-9_.-]", RegexOptions.Compiled);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly ConnectionFactory _factory;
    private readonly object _sync = new();
    private readonly TimeSpan _publisherConfirmTimeout;

    private IConnection? _connection;

    public RabbitMqMessageBus(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMessageBus> logger)
    {
        _options = options.Value;
        _logger = logger;

        ValidateOptions(_options);
        _publisherConfirmTimeout = TimeSpan.FromSeconds(Math.Max(1, _options.PublisherConfirmTimeoutSeconds));

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            ClientProvidedName = _options.ClientProvidedName,
            AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(Math.Max(1, _options.NetworkRecoveryIntervalSeconds))
        };

        EnsureConnection();
    }

    public Task PublishAsync(
        string? messageId,
        string messageType,
        string payload,
        string? headers = null,
        string? correlationId = null,
        string? causationId = null,
        string? idempotencyKey = null,
        int version = 1,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ct.ThrowIfCancellationRequested();

        var connection = EnsureConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: true,
            autoDelete: false);

        if (_options.PublisherConfirmsEnabled)
        {
            channel.ConfirmSelect();
        }

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = messageType;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.MessageId = messageId;
        properties.CorrelationId = correlationId;

        var metadataHeaders = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(headers))
        {
            metadataHeaders["headers_json"] = headers;
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            metadataHeaders["x-correlation-id"] = correlationId;
        }

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            metadataHeaders["x-causation-id"] = causationId;
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            metadataHeaders["x-idempotency-key"] = idempotencyKey;
        }

        metadataHeaders["x-version"] = version;

        if (metadataHeaders.Count > 0)
        {
            properties.Headers = metadataHeaders;
        }

        var body = Encoding.UTF8.GetBytes(payload);
        var routingKey = ResolveRoutingKey(messageType);

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        if (_options.PublisherConfirmsEnabled)
        {
            channel.WaitForConfirmsOrDie(_publisherConfirmTimeout);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            DisposeUnsafe();
        }
    }

    private IConnection EnsureConnection()
    {
        lock (_sync)
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            DisposeUnsafe();

            _connection = _factory.CreateConnection();

            _logger.LogInformation(
                "RabbitMQ message bus connected to {Host}:{Port}.",
                _options.HostName,
                _options.Port);

            return _connection;
        }
    }

    private string ResolveRoutingKey(string messageType)
    {
        if (!string.IsNullOrWhiteSpace(_options.DefaultRoutingKey))
        {
            return _options.DefaultRoutingKey;
        }

        var safeType = InvalidRoutingKeyCharacters.Replace(messageType, ".");
        return safeType.ToLowerInvariant();
    }

    private void DisposeUnsafe()
    {
        try
        {
            _connection?.Close();
        }
        catch
        {
        }
        finally
        {
            _connection?.Dispose();
            _connection = null;
        }
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:HostName is required.");
        }

        if (options.Port <= 0)
        {
            throw new InvalidOperationException("Messaging:RabbitMq:Port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:ExchangeName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeType))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:ExchangeType is required.");
        }

        if (options.PublisherConfirmsEnabled && options.PublisherConfirmTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Messaging:RabbitMq:PublisherConfirmTimeoutSeconds must be greater than zero when publisher confirms are enabled.");
        }
    }
}
