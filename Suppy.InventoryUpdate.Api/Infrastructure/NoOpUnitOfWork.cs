using Suppy.InventoryUpdate.Api.Abstractions.Persistence;

namespace Suppy.InventoryUpdate.Api.Infrastructure;

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public TRepository Get<TRepository>() where TRepository : notnull
    {
        throw new InvalidOperationException(
            "No persistence provider is configured. Configure SQL or Mongo persistence before resolving repositories.");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }

    public Task<bool> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<bool>> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action(ct);
    }

    public void ClearTracking()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
