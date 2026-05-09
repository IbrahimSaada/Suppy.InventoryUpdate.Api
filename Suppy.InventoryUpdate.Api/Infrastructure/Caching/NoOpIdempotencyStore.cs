using Suppy.InventoryUpdate.Api.Abstractions.Caching;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class NoOpIdempotencyStore : IIdempotencyStore
{
    public Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task MarkCompletedAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsCompletedAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
