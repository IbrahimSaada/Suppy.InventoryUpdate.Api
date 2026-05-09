namespace Suppy.InventoryUpdate.Api.Abstractions.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    TRepository Get<TRepository>() where TRepository : notnull;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<bool> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<bool>> action,
        CancellationToken ct = default);

    void ClearTracking();
}
