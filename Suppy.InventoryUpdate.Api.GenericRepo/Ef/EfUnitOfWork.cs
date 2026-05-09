using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Ef;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositoryCache = new();

    public EfUnitOfWork(DbContext dbContext, IServiceProvider serviceProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
        return _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<bool>> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await action(ct);
        }

        if (!_dbContext.Database.IsRelational())
        {
            return await action(ct);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            var shouldCommit = await action(ct);

            if (shouldCommit)
            {
                await transaction.CommitAsync(ct);
            }
            else
            {
                await transaction.RollbackAsync(ct);
                ClearTracking();
            }

            return shouldCommit;
        });
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}
