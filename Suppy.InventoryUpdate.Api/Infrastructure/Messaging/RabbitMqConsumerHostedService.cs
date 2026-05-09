using System.Text;
using System.Text.Json;
using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Infrastructure.Caching;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOptions<MessagingOptions> _messagingOptions;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _processingGate = new(1, 1);

    private IConnection? _connection;
    private IModel? _channel;
    private string? _consumerTag;
    private CancellationToken _serviceStoppingToken;

    public RabbitMqConsumerHostedService(
        IOptions<MessagingOptions> messagingOptions,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        IIdempotencyStore idempotencyStore,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        _messagingOptions = messagingOptions;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serviceStoppingToken = stoppingToken;

        if (!string.Equals(_messagingOptions.Value.Provider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("RabbitMQ consumer is disabled because Messaging:Provider is not RabbitMq.");
            return;
        }

        if (!_options.ConsumerEnabled)
        {
            _logger.LogInformation("RabbitMQ consumer is disabled (Messaging:RabbitMq:ConsumerEnabled=false).");
            return;
        }

        if (_idempotencyStore is InMemoryIdempotencyStore or NoOpIdempotencyStore)
        {
            _logger.LogWarning(
                "RabbitMQ consumer is using non-distributed idempotency store ({StoreType}). " +
                "This is suitable for local/dev only and is not safe for multi-instance processing.",
                _idempotencyStore.GetType().Name);
        }

        ValidateOptions(_options);
        EnsureConnectionAndTopology();

        var channel = _channel ?? throw new InvalidOperationException("RabbitMQ consumer channel is not initialized.");
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += HandleMessageAsync;

        _consumerTag = channel.BasicConsume(
            queue: _options.ConsumerQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started for queue {QueueName}.", _options.ConsumerQueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("RabbitMQ consumer stopping.");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            try
            {
                if (_channel?.IsOpen == true && !string.IsNullOrWhiteSpace(_consumerTag))
                {
                    _channel.BasicCancel(_consumerTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cancel RabbitMQ consumer cleanly.");
            }
            finally
            {
                _consumerTag = null;
                DisposeUnsafe();
            }
        }

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        lock (_sync)
        {
            DisposeUnsafe();
        }

        _processingGate.Dispose();
        base.Dispose();
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            await _processingGate.WaitAsync(_serviceStoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            var channel = _channel;
            if (channel is null || !channel.IsOpen)
            {
                return;
            }

            var correlationId =
                eventArgs.BasicProperties?.CorrelationId ??
                TryGetHeaderAsString(eventArgs.BasicProperties?.Headers, "x-correlation-id") ??
                eventArgs.BasicProperties?.MessageId;

            using var logScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId,
                ["MessageId"] = eventArgs.BasicProperties?.MessageId,
                ["MessageType"] = eventArgs.BasicProperties?.Type
            });

            var beganIdempotency = false;
            var idempotencyKey = string.Empty;

            try
            {
                var messageType = eventArgs.BasicProperties?.Type;
                if (string.IsNullOrWhiteSpace(messageType))
                {
                    _logger.LogWarning("Message received without type. DeliveryTag={DeliveryTag}", eventArgs.DeliveryTag);
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var eventType = Type.GetType(messageType, throwOnError: false);
                if (eventType is null || !typeof(IIntegrationEvent).IsAssignableFrom(eventType))
                {
                    _logger.LogWarning(
                        "Unsupported integration event type {MessageType}. Message sent to dead-letter queue.",
                        messageType);
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var deserialized = JsonSerializer.Deserialize(payload, eventType, PayloadJsonOptions) as IIntegrationEvent;
                if (deserialized is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize integration event payload for {MessageType}.",
                        messageType);
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                idempotencyKey = BuildIdempotencyKey(eventArgs, deserialized, messageType);
                var idempotencyTtl = TimeSpan.FromHours(Math.Max(1, _options.ConsumerIdempotencyTtlHours));

                var began = await _idempotencyStore.TryBeginAsync(idempotencyKey, idempotencyTtl, _serviceStoppingToken);
                if (!began)
                {
                    var completed = await _idempotencyStore.IsCompletedAsync(idempotencyKey, _serviceStoppingToken);
                    if (completed)
                    {
                        _logger.LogInformation(
                            "Duplicate message skipped by idempotency key {IdempotencyKey}.",
                            idempotencyKey);
                        channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                        return;
                    }

                    _logger.LogWarning(
                        "Message with idempotency key {IdempotencyKey} is already in progress. Requeueing.",
                        idempotencyKey);
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                beganIdempotency = true;

                await DispatchToConsumersAsync(eventType, deserialized, _serviceStoppingToken);

                await _idempotencyStore.MarkCompletedAsync(idempotencyKey, idempotencyTtl, _serviceStoppingToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                if (beganIdempotency && !string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    try
                    {
                        await _idempotencyStore.ReleaseAsync(idempotencyKey);
                    }
                    catch (Exception releaseEx)
                    {
                        _logger.LogWarning(
                            releaseEx,
                            "Failed to release idempotency key {IdempotencyKey} after consumer failure.",
                            idempotencyKey);
                    }
                }

                _logger.LogError(ex, "RabbitMQ message handling failed. Message moved to dead-letter queue.");
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
        finally
        {
            _processingGate.Release();
        }
    }

    private async Task DispatchToConsumersAsync(
        Type eventType,
        IIntegrationEvent integrationEvent,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var consumerInterface = typeof(IIntegrationEventConsumer<>).MakeGenericType(eventType);
        var consumers = scope.ServiceProvider.GetServices(consumerInterface).ToArray();

        if (consumers.Length == 0)
        {
            _logger.LogInformation(
                "No registered consumers for integration event type {EventType}. Message acknowledged.",
                eventType.FullName);
            return;
        }

        var consumeMethod = consumerInterface.GetMethod(nameof(IIntegrationEventConsumer<IIntegrationEvent>.ConsumeAsync));
        if (consumeMethod is null)
        {
            throw new InvalidOperationException(
                $"Cannot find {nameof(IIntegrationEventConsumer<IIntegrationEvent>.ConsumeAsync)} for {consumerInterface.Name}.");
        }

        foreach (var consumer in consumers)
        {
            var task = (Task?)consumeMethod.Invoke(consumer, [integrationEvent, ct]);
            if (task is not null)
            {
                await task;
            }
        }
    }

    private void EnsureConnectionAndTopology()
    {
        lock (_sync)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            {
                return;
            }

            DisposeUnsafe();

            var connectionFactory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = $"{_options.ClientProvidedName}.consumer",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Math.Max(1, _options.NetworkRecoveryIntervalSeconds))
            };

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(prefetchSize: 0, prefetchCount: _options.ConsumerPrefetchCount, global: false);

            _channel.ExchangeDeclare(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType,
                durable: true,
                autoDelete: false);

            _channel.ExchangeDeclare(
                exchange: _options.DeadLetterExchangeName,
                type: "topic",
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: _options.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: _options.DeadLetterQueueName,
                exchange: _options.DeadLetterExchangeName,
                routingKey: _options.DeadLetterRoutingKey);

            var arguments = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = _options.DeadLetterRoutingKey
            };

            _channel.QueueDeclare(
                queue: _options.ConsumerQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            var routingKeys = _options.ConsumerRoutingKeys.Length == 0 ? ["#"] : _options.ConsumerRoutingKeys;
            foreach (var routingKey in routingKeys)
            {
                _channel.QueueBind(
                    queue: _options.ConsumerQueueName,
                    exchange: _options.ExchangeName,
                    routingKey: routingKey);
            }
        }
    }

    private void DisposeUnsafe()
    {
        try
        {
            _channel?.Close();
        }
        catch
        {
        }
        finally
        {
            _channel?.Dispose();
            _channel = null;
        }

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

    private static string BuildIdempotencyKey(
        BasicDeliverEventArgs eventArgs,
        IIntegrationEvent integrationEvent,
        string messageType)
    {
        if (!string.IsNullOrWhiteSpace(integrationEvent.IdempotencyKey))
        {
            return $"integration:{integrationEvent.IdempotencyKey}";
        }

        var messageId = eventArgs.BasicProperties?.MessageId;
        if (!string.IsNullOrWhiteSpace(messageId))
        {
            return $"message:{messageId}";
        }

        return $"event:{messageType}:{integrationEvent.EventId:N}";
    }

    private static string? TryGetHeaderAsString(
        IDictionary<string, object>? headers,
        string headerName)
    {
        if (headers is null || !headers.TryGetValue(headerName, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            string str => str,
            _ => value.ToString()
        };
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConsumerQueueName))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:ConsumerQueueName is required.");
        }

        if (options.ConsumerPrefetchCount == 0)
        {
            throw new InvalidOperationException("Messaging:RabbitMq:ConsumerPrefetchCount must be greater than zero.");
        }

        if (options.ConsumerPrefetchCount > 1)
        {
            throw new InvalidOperationException(
                "Messaging:RabbitMq:ConsumerPrefetchCount must be 1 for the template consumer baseline.");
        }

        if (string.IsNullOrWhiteSpace(options.DeadLetterExchangeName))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:DeadLetterExchangeName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DeadLetterQueueName))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:DeadLetterQueueName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DeadLetterRoutingKey))
        {
            throw new InvalidOperationException("Messaging:RabbitMq:DeadLetterRoutingKey is required.");
        }

        if (options.ConsumerIdempotencyTtlHours <= 0)
        {
            throw new InvalidOperationException("Messaging:RabbitMq:ConsumerIdempotencyTtlHours must be greater than zero.");
        }
    }
}
