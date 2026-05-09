using System.Collections.Concurrent;
using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Suppy.InventoryUpdate.Api.Abstractions.Clock;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly IClock _clock;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ConcurrentDictionary<string, Entry> _entries =
        new(StringComparer.Ordinal);

    public InMemoryIdempotencyStore(
        IClock clock,
        ICacheKeyFactory cacheKeyFactory)
    {
        _clock = clock;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        ValidateInputs(key, ttl);
        ct.ThrowIfCancellationRequested();

        var storageKey = BuildStorageKey(key);
        var now = _clock.UtcNow;
        var expiresAtUtc = now.Add(ttl);

        while (true)
        {
            if (_entries.TryGetValue(storageKey, out var existing))
            {
                if (existing.ExpiresAtUtc <= now)
                {
                    _entries.TryRemove(storageKey, out _);
                    continue;
                }

                return Task.FromResult(false);
            }

            var created = _entries.TryAdd(storageKey, new Entry("in_progress", expiresAtUtc));
            if (created)
            {
                return Task.FromResult(true);
            }
        }
    }

    public Task MarkCompletedAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        ValidateInputs(key, ttl);
        ct.ThrowIfCancellationRequested();

        var storageKey = BuildStorageKey(key);
        var expiresAtUtc = _clock.UtcNow.Add(ttl);
        _entries[storageKey] = new Entry("completed", expiresAtUtc);

        return Task.CompletedTask;
    }

    public Task<bool> IsCompletedAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        ct.ThrowIfCancellationRequested();

        var storageKey = BuildStorageKey(key);
        if (!_entries.TryGetValue(storageKey, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAtUtc <= _clock.UtcNow)
        {
            _entries.TryRemove(storageKey, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(string.Equals(entry.Status, "completed", StringComparison.OrdinalIgnoreCase));
    }

    public Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        ct.ThrowIfCancellationRequested();

        var storageKey = BuildStorageKey(key);
        _entries.TryRemove(storageKey, out _);

        return Task.CompletedTask;
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

    private sealed record Entry(string Status, DateTime ExpiresAtUtc);
}
