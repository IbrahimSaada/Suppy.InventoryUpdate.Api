using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class RedisCacheStore : ICacheStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ICacheSerializer _serializer;
    private readonly RedisOptions _options;

    public RedisCacheStore(
        IConnectionMultiplexer connectionMultiplexer,
        ICacheSerializer serializer,
        IOptions<RedisOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _serializer = serializer;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ValidateKey(key);
        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        var payload = await db.StringGetAsync(key).ConfigureAwait(false);
        if (payload.IsNullOrEmpty)
        {
            return default;
        }

        var bytes = (byte[]?)payload;
        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        return _serializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        ValidateKey(key);
        ct.ThrowIfCancellationRequested();

        var ttl = options?.AbsoluteExpirationRelativeToNow ??
                  TimeSpan.FromSeconds(Math.Max(1, _options.DefaultTtlSeconds));

        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentException("Cache TTL must be greater than zero.", nameof(options));
        }

        var db = _connectionMultiplexer.GetDatabase();
        var payload = _serializer.Serialize(value);
        await db.StringSetAsync(key, payload, ttl).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);
        ct.ThrowIfCancellationRequested();

        var db = _connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key is required.", nameof(key));
        }
    }
}
