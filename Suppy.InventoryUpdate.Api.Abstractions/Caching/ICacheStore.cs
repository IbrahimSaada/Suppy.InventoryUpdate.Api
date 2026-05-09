namespace Suppy.InventoryUpdate.Api.Abstractions.Caching;

public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
