using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using StackExchange.Redis;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ICacheSerializer _cacheSerializer;

    public RedisIdempotencyStore(
        IConnectionMultiplexer connectionMultiplexer,
        ICacheKeyFactory cacheKeyFactory,
        ICacheSerializer cacheSerializer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _cacheKeyFactory = cacheKeyFactory;
        _cacheSerializer = cacheSerializer;
    }

    public async Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        ValidateInputs(key, ttl);
        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        var storageKey = BuildStorageKey(key);
        var payload = _cacheSerializer.Serialize(new IdempotencyRecord("in_progress"));

        return await db.StringSetAsync(storageKey, payload, ttl, when: When.NotExists).ConfigureAwait(false);
    }

    public async Task MarkCompletedAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        ValidateInputs(key, ttl);
        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        var storageKey = BuildStorageKey(key);
        var payload = _cacheSerializer.Serialize(new IdempotencyRecord("completed"));
        await db.StringSetAsync(storageKey, payload, ttl).ConfigureAwait(false);
    }

    public async Task<bool> IsCompletedAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        var storageKey = BuildStorageKey(key);
        var payload = await db.StringGetAsync(storageKey).ConfigureAwait(false);
        if (payload.IsNullOrEmpty)
        {
            return false;
        }

        var bytes = (byte[]?)payload;
        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        var record = _cacheSerializer.Deserialize<IdempotencyRecord>(bytes);
        return string.Equals(record?.Status, "completed", StringComparison.OrdinalIgnoreCase);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        var storageKey = BuildStorageKey(key);
        await db.KeyDeleteAsync(storageKey).ConfigureAwait(false);
    }

    private string BuildStorageKey(string key)
    {
        return _cacheKeyFactory.Create(CacheCategories.Idempotency, key);
    }

    private static void ValidateInputs(string key, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentException("Idempotency TTL must be greater than zero.", nameof(ttl));
        }
    }

    private sealed record IdempotencyRecord(string Status);
}
