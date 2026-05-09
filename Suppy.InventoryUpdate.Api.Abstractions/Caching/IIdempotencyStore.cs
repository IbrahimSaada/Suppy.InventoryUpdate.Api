namespace Suppy.InventoryUpdate.Api.Abstractions.Caching;

public interface IIdempotencyStore
{
    Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    Task MarkCompletedAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    Task<bool> IsCompletedAsync(string key, CancellationToken ct = default);

    Task ReleaseAsync(string key, CancellationToken ct = default);
}
