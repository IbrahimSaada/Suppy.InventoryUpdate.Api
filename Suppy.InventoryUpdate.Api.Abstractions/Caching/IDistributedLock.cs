namespace Suppy.InventoryUpdate.Api.Abstractions.Caching;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan leaseTime,
        CancellationToken ct = default);
}
