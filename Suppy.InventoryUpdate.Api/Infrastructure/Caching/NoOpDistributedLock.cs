using Suppy.InventoryUpdate.Api.Abstractions.Caching;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class NoOpDistributedLock : IDistributedLock
{
    public Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan leaseTime,
        CancellationToken ct = default)
    {
        return Task.FromResult<IAsyncDisposable?>(null);
    }
}
