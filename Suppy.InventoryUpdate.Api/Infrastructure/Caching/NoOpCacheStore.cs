using Suppy.InventoryUpdate.Api.Abstractions.Caching;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class NoOpCacheStore : ICacheStore
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
