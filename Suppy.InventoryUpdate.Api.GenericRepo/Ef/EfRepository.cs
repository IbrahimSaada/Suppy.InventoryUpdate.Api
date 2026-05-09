using System.Linq.Expressions;
using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Paging;
using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Suppy.InventoryUpdate.Api.Domain.Entities;
using Suppy.InventoryUpdate.Api.GenericRepo.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Ef;

public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private const string IsDeletedProperty = "IsDeleted";
    private const string DeletedAtUtcProperty = "DeletedAtUtc";
    private const string UpdatedAtUtcProperty = "UpdatedAtUtc";

    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _set;
    private readonly IClock _clock;

    public EfRepository(DbContext dbContext, IClock clock)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _set = _dbContext.Set<TEntity>();
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public IQueryable<TEntity> Query(bool asNoTracking = true, bool includeDeleted = false)
    {
        IQueryable<TEntity> query = _set;

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        else if (EntityMetadata<TEntity>.SupportsSoftDelete)
        {
            query = query.Where(EntityMetadata<TEntity>.NotDeletedPredicate!);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    public Task<TEntity?> GetByIdAsync(
        TKey id,
        bool includeDeleted = false,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        return Query(asNoTracking, includeDeleted)
            .FirstOrDefaultAsync(entity => entity.Id.Equals(id), ct);
    }

    public async Task<IReadOnlyList<TEntity>> GetManyByIdsAsync(
        IEnumerable<TKey> ids,
        bool includeDeleted = false,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var keyArray = ids.Distinct().ToArray();
        if (keyArray.Length == 0)
        {
            return Array.Empty<TEntity>();
        }

        return await Query(asNoTracking, includeDeleted)
            .Where(entity => keyArray.Contains(entity.Id))
            .ToListAsync(ct);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = Query(asNoTracking, includeDeleted)
            .ApplyIncludes(includes)
            .Where(predicate);

        return query.FirstOrDefaultAsync(ct);
    }

    public Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(selector);

        var query = Query(asNoTracking, includeDeleted)
            .ApplyIncludes(includes)
            .Where(predicate)
            .Select(selector);

        return query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int? skip = null,
        int? take = null,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        IQueryable<TEntity> query = Query(asNoTracking, includeDeleted)
            .ApplyIncludes(includes)
            .Where(predicate)
            .ApplyOrdering(Array.Empty<SortRule<TEntity>>(), entity => entity.Id, EntityMetadata<TEntity>.HasCreatedAtUtc);

        if (skip is > 0)
        {
            query = query.Skip(skip.Value);
        }

        if (take is > 0)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        int? skip = null,
        int? take = null,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TEntity> query = Query(asNoTracking, includeDeleted)
            .ApplyIncludes(includes)
            .Where(predicate)
            .ApplyOrdering(Array.Empty<SortRule<TEntity>>(), entity => entity.Id, EntityMetadata<TEntity>.HasCreatedAtUtc);

        if (skip is > 0)
        {
            query = query.Skip(skip.Value);
        }

        if (take is > 0)
        {
            query = query.Take(take.Value);
        }

        return await query
            .Select(selector)
            .ToListAsync(ct);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query(asNoTracking: true, includeDeleted)
            .AnyAsync(predicate, ct);
    }

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query(asNoTracking: true, includeDeleted)
            .CountAsync(predicate, ct);
    }

    public Task<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query(asNoTracking: true, includeDeleted)
            .LongCountAsync(predicate, ct);
    }

    public async Task AddAsync(
        TEntity entity,
        bool assignTimestamps = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (assignTimestamps)
        {
            AuditStampApplier.ApplyCreationStamps(entity, _clock.UtcNow);
        }

        await _set.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        bool assignTimestamps = true,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityArray = entities.ToArray();
        if (entityArray.Length == 0)
        {
            return;
        }

        if (assignTimestamps)
        {
            var utcNow = _clock.UtcNow;
            foreach (var entity in entityArray)
            {
                AuditStampApplier.ApplyCreationStamps(entity, utcNow);
            }
        }

        await _set.AddRangeAsync(entityArray, ct);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entry = _dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                $"Detached update is blocked for {typeof(TEntity).Name}. Load tracked entity first or use PatchAsync.");
        }

        AuditStampApplier.TouchUpdatedAt(entity, _clock.UtcNow);
    }

    public async Task<bool> PatchAsync(
        TKey id,
        Action<TEntity> patch,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        var trackedEntity = await Query(asNoTracking: false, includeDeleted: true)
            .FirstOrDefaultAsync(entity => entity.Id.Equals(id), ct);

        if (trackedEntity is null)
        {
            return false;
        }

        patch(trackedEntity);
        AuditStampApplier.TouchUpdatedAt(trackedEntity, _clock.UtcNow);

        return true;
    }

    public async Task<bool> SoftDeleteByIdAsync(
        TKey id,
        CancellationToken ct = default)
    {
        EnsureSoftDeleteSupported();

        var utcNow = _clock.UtcNow;
        var affectedRows = await Query(asNoTracking: false, includeDeleted: false)
            .Where(entity => entity.Id.Equals(id))
            .ExecuteUpdateAsync(BuildSoftDeleteSetters(utcNow), ct);

        return affectedRows > 0;
    }

    public async Task<long> SoftDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureSoftDeleteSupported();

        var utcNow = _clock.UtcNow;

        return await Query(asNoTracking: false, includeDeleted: false)
            .Where(predicate)
            .ExecuteUpdateAsync(BuildSoftDeleteSetters(utcNow), ct);
    }

    public async Task<long> HardDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await Query(asNoTracking: false, includeDeleted: true)
            .Where(predicate)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<CursorPageResult<TResult>> CursorPaginatedAsync<TResult>(
        CursorQuery<TEntity, TResult> query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = NormalizeLimit(query.Page.Limit, query.MaxLimit);
        var offset = CursorTokenSerializer.DecodeOffset(query.Page.Cursor, query.ProtectionKey);

        var sourceQuery = BuildReadQuery(
            query.Predicate,
            query.IncludeDeleted,
            query.AsNoTracking,
            query.Includes,
            query.Tag);

        var orderedQuery = sourceQuery.ApplyOrdering(
            query.OrderBy,
            entity => entity.Id,
            EntityMetadata<TEntity>.HasCreatedAtUtc);

        var rows = await orderedQuery
            .Skip(offset)
            .Take(limit + 1)
            .Select(query.Selector)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows = rows.Take(limit).ToList();
        }

        var nextCursor = hasMore
            ? CursorTokenSerializer.EncodeOffset(offset + rows.Count, query.ProtectionKey)
            : null;

        return new CursorPageResult<TResult>(rows, nextCursor, rows.Count, hasMore);
    }

    public async Task<OffsetPageResult<TResult>> PaginatedAsync<TResult>(
        OffsetQuery<TEntity, TResult> query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page.Page, 1);
        var pageSize = Math.Max(query.Page.PageSize, 1);
        var skip = (page - 1) * pageSize;

        var sourceForCount = BuildReadQuery(
            query.Predicate,
            query.IncludeDeleted,
            asNoTracking: true,
            includes: null,
            tag: null);

        var totalCount = await sourceForCount.LongCountAsync(ct);
        if (totalCount > 0 && skip >= totalCount)
        {
            return new OffsetPageResult<TResult>(Array.Empty<TResult>(), totalCount, page, pageSize);
        }

        var sourceForItems = BuildReadQuery(
            query.Predicate,
            query.IncludeDeleted,
            query.AsNoTracking,
            query.Includes,
            tag: null);

        var orderedQuery = sourceForItems.ApplyOrdering(
            query.OrderBy,
            entity => entity.Id,
            EntityMetadata<TEntity>.HasCreatedAtUtc);

        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(query.Selector)
            .ToListAsync(ct);

        return new OffsetPageResult<TResult>(items, totalCount, page, pageSize);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await action(ct);
            return;
        }

        if (!_dbContext.Database.IsRelational())
        {
            await action(ct);
            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            await action(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private IQueryable<TEntity> BuildReadQuery(
        Expression<Func<TEntity, bool>>? predicate,
        bool includeDeleted,
        bool asNoTracking,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes,
        string? tag)
    {
        var query = Query(asNoTracking, includeDeleted);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.TagWith(tag);
        }

        query = query.ApplyIncludes(includes);

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }

    private static int NormalizeLimit(int requestedLimit, int maxLimit)
    {
        var normalizedMax = Math.Max(maxLimit, 1);
        var normalizedRequested = Math.Max(requestedLimit, 1);
        return Math.Min(normalizedRequested, normalizedMax);
    }

    private static Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> BuildSoftDeleteSetters(DateTime utcNow)
    {
        if (EntityMetadata<TEntity>.UpdatedAtUtcType == typeof(DateTime))
        {
            return setters => setters
                .SetProperty(entity => EF.Property<bool>(entity, IsDeletedProperty), _ => true)
                .SetProperty(entity => EF.Property<DateTime?>(entity, DeletedAtUtcProperty), _ => utcNow)
                .SetProperty(entity => EF.Property<DateTime>(entity, UpdatedAtUtcProperty), _ => utcNow);
        }

        if (EntityMetadata<TEntity>.UpdatedAtUtcType == typeof(DateTime?))
        {
            return setters => setters
                .SetProperty(entity => EF.Property<bool>(entity, IsDeletedProperty), _ => true)
                .SetProperty(entity => EF.Property<DateTime?>(entity, DeletedAtUtcProperty), _ => utcNow)
                .SetProperty(entity => EF.Property<DateTime?>(entity, UpdatedAtUtcProperty), _ => utcNow);
        }

        return setters => setters
            .SetProperty(entity => EF.Property<bool>(entity, IsDeletedProperty), _ => true)
            .SetProperty(entity => EF.Property<DateTime?>(entity, DeletedAtUtcProperty), _ => utcNow);
    }

    private static void EnsureSoftDeleteSupported()
    {
        if (!EntityMetadata<TEntity>.SupportsSoftDelete)
        {
            throw new NotSupportedException(
                $"Soft delete is not supported for {typeof(TEntity).Name}. Ensure IsDeleted and DeletedAtUtc properties exist.");
        }
    }
}
