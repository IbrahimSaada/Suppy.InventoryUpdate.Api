using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using StackExchange.Redis;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public RedisDistributedLock(
        IConnectionMultiplexer connectionMultiplexer,
        ICacheKeyFactory cacheKeyFactory)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan leaseTime,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Lock key is required.", nameof(key));
        }

        if (leaseTime <= TimeSpan.Zero)
        {
            throw new ArgumentException("Lock lease time must be greater than zero.", nameof(leaseTime));
        }

        ct.ThrowIfCancellationRequested();

        var token = Guid.NewGuid().ToString("N");
        var storageKey = _cacheKeyFactory.Create(CacheCategories.Lock, key);
        var db = _connectionMultiplexer.GetDatabase();
        var acquired = await db.StringSetAsync(
                storageKey,
                token,
                leaseTime,
                when: When.NotExists)
            .ConfigureAwait(false);

        if (!acquired)
        {
            return null;
        }

        return new RedisDistributedLockHandle(db, storageKey, token);
    }

    private sealed class RedisDistributedLockHandle : IAsyncDisposable
    {
        private const string ReleaseScript = """
                                             if redis.call('GET', KEYS[1]) == ARGV[1] then
                                                 return redis.call('DEL', KEYS[1])
                                             else
                                                 return 0
                                             end
                                             """;

        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _token;
        private bool _released;

        public RedisDistributedLockHandle(
            IDatabase database,
            RedisKey key,
            RedisValue token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            await _database.ScriptEvaluateAsync(
                    ReleaseScript,
                    new[] { _key },
                    new[] { _token })
                .ConfigureAwait(false);
        }
    }
}
