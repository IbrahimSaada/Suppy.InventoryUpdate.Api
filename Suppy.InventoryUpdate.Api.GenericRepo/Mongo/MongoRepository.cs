using System.Linq.Expressions;
using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Paging;
using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Suppy.InventoryUpdate.Api.Domain.Entities;
using Suppy.InventoryUpdate.Api.GenericRepo.Common;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Mongo;

public class MongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private const string IsDeletedProperty = "IsDeleted";
    private const string DeletedAtUtcProperty = "DeletedAtUtc";
    private const string UpdatedAtUtcProperty = "UpdatedAtUtc";

    private readonly IMongoCollection<TEntity> _collection;
    private readonly IClock _clock;

    public MongoRepository(
        IMongoDatabase database,
        IClock clock,
        IMongoCollectionNameResolver collectionNameResolver)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(collectionNameResolver);

        _clock = clock;
        var collectionName = collectionNameResolver.Resolve<TEntity>();
        _collection = database.GetCollection<TEntity>(collectionName);
    }

    public IQueryable<TEntity> Query(bool asNoTracking = true, bool includeDeleted = false)
    {
        var query = _collection.AsQueryable();

        if (!includeDeleted && EntityMetadata<TEntity>.SupportsSoftDelete)
        {
            query = query.Where(EntityMetadata<TEntity>.NotDeletedPredicate!);
        }

        return query;
    }

    public async Task<TEntity?> GetByIdAsync(
        TKey id,
        bool includeDeleted = false,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = Query(asNoTracking, includeDeleted)
            .Where(entity => entity.Id.Equals(id));

        TEntity? entity = await AsMongoQueryable(query).FirstOrDefaultAsync(ct);
        return entity;
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

        var query = Query(asNoTracking, includeDeleted)
            .Where(entity => keyArray.Contains(entity.Id));

        return await AsMongoQueryable(query).ToListAsync(ct);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        bool asNoTracking = true,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = Query(asNoTracking, includeDeleted)
            .Where(predicate);

        TEntity? entity = await AsMongoQueryable(query).FirstOrDefaultAsync(ct);
        return entity;
    }

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(
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
            .Where(predicate)
            .Select(selector);

        TResult? result = await AsMongoQueryable(query).FirstOrDefaultAsync(ct);
        return result;
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

        return await AsMongoQueryable(query).ToListAsync(ct);
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

        return await AsMongoQueryable(query.Select(selector)).ToListAsync(ct);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = Query(asNoTracking: true, includeDeleted)
            .Where(predicate);

        return AsMongoQueryable(query).AnyAsync(ct);
    }

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = Query(asNoTracking: true, includeDeleted)
            .Where(predicate);

        return AsMongoQueryable(query).CountAsync(ct);
    }

    public Task<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = Query(asNoTracking: true, includeDeleted)
            .Where(predicate);

        return AsMongoQueryable(query).LongCountAsync(ct);
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

        await _collection.InsertOneAsync(entity, cancellationToken: ct);
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

        await _collection.InsertManyAsync(entityArray, cancellationToken: ct);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        AuditStampApplier.TouchUpdatedAt(entity, _clock.UtcNow);
        _collection.ReplaceOne(
            Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id),
            entity,
            new ReplaceOptions { IsUpsert = false });
    }

    public async Task<bool> PatchAsync(
        TKey id,
        Action<TEntity> patch,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        var query = Query(asNoTracking: true, includeDeleted: true)
            .Where(document => document.Id.Equals(id));

        var entity = await AsMongoQueryable(query).FirstOrDefaultAsync(ct);
        if (entity is null)
        {
            return false;
        }

        patch(entity);
        AuditStampApplier.TouchUpdatedAt(entity, _clock.UtcNow);

        var result = await _collection.ReplaceOneAsync(
            Builders<TEntity>.Filter.Eq(document => document.Id, id),
            entity,
            new ReplaceOptions { IsUpsert = false },
            ct);

        return result.MatchedCount > 0;
    }

    public async Task<bool> SoftDeleteByIdAsync(
        TKey id,
        CancellationToken ct = default)
    {
        EnsureSoftDeleteSupported();

        var filter = Builders<TEntity>.Filter.Eq(entity => entity.Id, id);
        var update = BuildSoftDeleteUpdate(_clock.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<long> SoftDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureSoftDeleteSupported();

        var filter = Builders<TEntity>.Filter.Where(predicate);
        var update = BuildSoftDeleteUpdate(_clock.UtcNow);

        var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }

    public async Task<long> HardDeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var filter = Builders<TEntity>.Filter.Where(predicate);
        var result = await _collection.DeleteManyAsync(filter, ct);

        return result.DeletedCount;
    }

    public async Task<CursorPageResult<TResult>> CursorPaginatedAsync<TResult>(
        CursorQuery<TEntity, TResult> query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = NormalizeLimit(query.Page.Limit, query.MaxLimit);
        var offset = CursorTokenSerializer.DecodeOffset(query.Page.Cursor, query.ProtectionKey);

        var sourceQuery = BuildReadQuery(query.Predicate, query.IncludeDeleted, query.AsNoTracking);

        var orderedQuery = sourceQuery.ApplyOrdering(
            query.OrderBy,
            entity => entity.Id,
            EntityMetadata<TEntity>.HasCreatedAtUtc);

        var rows = await AsMongoQueryable(
                orderedQuery
                    .Skip(offset)
                    .Take(limit + 1)
                    .Select(query.Selector))
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

        var sourceForCount = BuildReadQuery(query.Predicate, query.IncludeDeleted, asNoTracking: true);
        var totalCount = await AsMongoQueryable(sourceForCount).LongCountAsync(ct);
        if (totalCount > 0 && skip >= totalCount)
        {
            return new OffsetPageResult<TResult>(Array.Empty<TResult>(), totalCount, page, pageSize);
        }

        var sourceForItems = BuildReadQuery(query.Predicate, query.IncludeDeleted, query.AsNoTracking);
        var orderedQuery = sourceForItems.ApplyOrdering(
            query.OrderBy,
            entity => entity.Id,
            EntityMetadata<TEntity>.HasCreatedAtUtc);

        var items = await AsMongoQueryable(
                orderedQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(query.Selector))
            .ToListAsync(ct);

        return new OffsetPageResult<TResult>(items, totalCount, page, pageSize);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        // Transaction scope for Mongo requires session-aware operations.
        // The repository interface does not carry session context, so we execute directly.
        await action(ct);
    }

    private IQueryable<TEntity> BuildReadQuery(
        Expression<Func<TEntity, bool>>? predicate,
        bool includeDeleted,
        bool asNoTracking)
    {
        var query = Query(asNoTracking, includeDeleted);

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }

    private static IMongoQueryable<TDocument> AsMongoQueryable<TDocument>(IQueryable<TDocument> query)
    {
        return query as IMongoQueryable<TDocument>
            ?? throw new InvalidOperationException("Query provider is not a Mongo provider.");
    }

    private static int NormalizeLimit(int requestedLimit, int maxLimit)
    {
        var normalizedMax = Math.Max(maxLimit, 1);
        var normalizedRequested = Math.Max(requestedLimit, 1);
        return Math.Min(normalizedRequested, normalizedMax);
    }

    private static UpdateDefinition<TEntity> BuildSoftDeleteUpdate(DateTime utcNow)
    {
        var updates = new List<UpdateDefinition<TEntity>>
        {
            Builders<TEntity>.Update.Set(IsDeletedProperty, true),
            Builders<TEntity>.Update.Set<DateTime?>(DeletedAtUtcProperty, utcNow)
        };

        if (EntityMetadata<TEntity>.UpdatedAtUtcType == typeof(DateTime))
        {
            updates.Add(Builders<TEntity>.Update.Set<DateTime>(UpdatedAtUtcProperty, utcNow));
        }
        else if (EntityMetadata<TEntity>.UpdatedAtUtcType == typeof(DateTime?))
        {
            updates.Add(Builders<TEntity>.Update.Set<DateTime?>(UpdatedAtUtcProperty, utcNow));
        }

        return Builders<TEntity>.Update.Combine(updates);
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

