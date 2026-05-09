using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Mongo;

public sealed class MongoUnitOfWork : IUnitOfWork
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositoryCache = new();

    public MongoUnitOfWork(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TRepository Get<TRepository>() where TRepository : notnull
    {
        var repositoryType = typeof(TRepository);
        if (_repositoryCache.TryGetValue(repositoryType, out var cachedRepository))
        {
            return (TRepository)cachedRepository;
        }

        var resolvedRepository = _serviceProvider.GetRequiredService<TRepository>();
        _repositoryCache[repositoryType] = resolvedRepository;

        return resolvedRepository;
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

        // Transactions for Mongo require session-aware repository operations.
        // Current abstraction does not flow a session, so this executes directly.
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
