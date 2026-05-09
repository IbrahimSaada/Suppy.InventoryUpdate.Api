using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var db = _connectionMultiplexer.GetDatabase();
            var latency = await db.PingAsync().ConfigureAwait(false);
            var isConnected = _connectionMultiplexer.IsConnected;

            if (!isConnected)
            {
                return HealthCheckResult.Unhealthy("Redis multiplexer is not connected.");
            }

            return HealthCheckResult.Healthy($"Redis ping latency: {latency.TotalMilliseconds:N0}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
        }
    }
}
