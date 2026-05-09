using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Microsoft.Extensions.Options;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly OutboxDispatcherOptions _options;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IMessageBus messageBus,
        IOptions<OutboxDispatcherOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _messageBus = messageBus;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox dispatcher is disabled.");
            return;
        }

        ValidateOptions(_options);
        _logger.LogInformation("Outbox dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox dispatcher stopped.");
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var now = _clock.UtcNow;
        var lockId = Guid.NewGuid().ToString("N");
        var lockTimeout = TimeSpan.FromSeconds(_options.LockTimeoutSeconds);

        var messages = await outboxStore.ClaimBatchAsync(
            batchSize: _options.BatchSize,
            lockId: lockId,
            utcNow: now,
            lockTimeout: lockTimeout,
            ct: ct);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await _messageBus.PublishAsync(
                    messageId: message.MessageId.ToString("N"),
                    messageType: message.MessageType,
                    payload: message.Payload,
                    headers: message.Headers,
                    correlationId: message.CorrelationId,
                    causationId: message.CausationId,
                    idempotencyKey: message.IdempotencyKey,
                    version: message.Version,
                    ct: ct);

                _logger.LogInformation(
                    "Outbox message {MessageId} ({MessageType}) dispatched.",
                    message.MessageId,
                    message.MessageType);

                await outboxStore.MarkSucceededAsync(
                    messageId: message.MessageId,
                    lockId: lockId,
                    processedAtUtc: _clock.UtcNow,
                    ct: ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                var nextRetry = message.RetryCount + 1;
                var moveToPoison = nextRetry >= _options.MaxRetryCount;
                var backoffSeconds = CalculateBackoffSeconds(nextRetry);
                var nextAvailableAtUtc = _clock.UtcNow.AddSeconds(backoffSeconds);
                var safeError = BuildSafeError(ex);

                await outboxStore.MarkFailedAsync(
                    messageId: message.MessageId,
                    lockId: lockId,
                    error: safeError,
                    nextAvailableAtUtc: nextAvailableAtUtc,
                    moveToPoison: moveToPoison,
                    ct: ct);

                _logger.LogError(
                    ex,
                    "Outbox message {MessageId} failed dispatch. Retry {RetryCount}, poison: {IsPoison}.",
                    message.MessageId,
                    nextRetry,
                    moveToPoison);
            }
        }
    }

    private int CalculateBackoffSeconds(int retryCount)
    {
        var safeRetry = Math.Max(1, retryCount);
        var exponent = Math.Min(8, safeRetry - 1);
        var baseSeconds = Math.Max(1, _options.BaseBackoffSeconds);

        return baseSeconds * (1 << exponent);
    }

    private string BuildSafeError(Exception ex)
    {
        var rawError = ex.ToString();
        if (string.IsNullOrWhiteSpace(rawError))
        {
            return "Outbox dispatch failed.";
        }

        var maxLength = Math.Max(1, _options.MaxErrorLength);
        return rawError[..Math.Min(rawError.Length, maxLength)];
    }

    private static void ValidateOptions(OutboxDispatcherOptions options)
    {
        if (options.BatchSize <= 0)
        {
            throw new InvalidOperationException("Messaging:Outbox:BatchSize must be greater than zero.");
        }

        if (options.PollIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Messaging:Outbox:PollIntervalSeconds must be greater than zero.");
        }

        if (options.LockTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Messaging:Outbox:LockTimeoutSeconds must be greater than zero.");
        }

        if (options.MaxRetryCount <= 0)
        {
            throw new InvalidOperationException("Messaging:Outbox:MaxRetryCount must be greater than zero.");
        }

        if (options.MaxErrorLength <= 0)
        {
            throw new InvalidOperationException("Messaging:Outbox:MaxErrorLength must be greater than zero.");
        }
    }
}
