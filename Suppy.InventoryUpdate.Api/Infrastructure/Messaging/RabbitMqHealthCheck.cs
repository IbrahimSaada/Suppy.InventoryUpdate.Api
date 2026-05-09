using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IOptions<MessagingOptions> _messagingOptions;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;

    public RabbitMqHealthCheck(
        IOptions<MessagingOptions> messagingOptions,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _messagingOptions = messagingOptions;
        _rabbitMqOptions = rabbitMqOptions;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(_messagingOptions.Value.Provider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ health check skipped (provider is not RabbitMq)."));
            }

            var options = _rabbitMqOptions.Value;

            var connectionFactory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                AutomaticRecoveryEnabled = false
            };

            using var connection = connectionFactory.CreateConnection();
            if (!connection.IsOpen)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not open."));
            }

            using var channel = connection.CreateModel();
            channel.ExchangeDeclarePassive(options.ExchangeName);

            return Task.FromResult(HealthCheckResult.Healthy(
                $"RabbitMQ reachable at {options.HostName}:{options.Port}, exchange '{options.ExchangeName}' is available."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check failed.", ex));
        }
    }
}
