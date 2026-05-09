using System.Linq.Expressions;
using Suppy.InventoryUpdate.Api.Abstractions.Paging;

namespace Suppy.InventoryUpdate.Api.Abstractions.Persistence;

public interface IRepository<TEntity, in TKey>
    where TEntity : class
    where TKey : notnull
{
    IQueryable<TEntity> Query(bool asNoTracking = true, bool includeDeleted = false);

    Task<TEntity?> GetByIdAsync(
        TKey id,
        bool includeDeleted = false,
        bool asNoTracking = true,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> GetManyByIdsAsync(
        IEnumerable<TKey> ids,
        bool includeDeleted = false,
        bool asNoTracking = true,
        CancellationToken ct = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default);

    Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int? skip = null,
        int? take = null,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        int? skip = null,
        int? take = null,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task AddAsync(
        TEntity entity,
        bool assignTimestamps = true,
        CancellationToken ct = default);

    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        bool assignTimestamps = true,
        CancellationToken ct = default);

    void Update(TEntity entity);

    Task<bool> PatchAsync(
        TKey id,
        Action<TEntity> patch,
        CancellationToken ct = default);

    Task<bool> SoftDeleteByIdAsync(
        TKey id,
        CancellationToken ct = default);

    Task<long> SoftDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<long> HardDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<CursorPageResult<TResult>> CursorPaginatedAsync<TResult>(
        CursorQuery<TEntity, TResult> query,
        CancellationToken ct = default);

    Task<OffsetPageResult<TResult>> PaginatedAsync<TResult>(
        OffsetQuery<TEntity, TResult> query,
        CancellationToken ct = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default);
}
