using System.Linq.Expressions;

namespace Suppy.InventoryUpdate.Api.Abstractions.Paging;

public sealed class CursorQuery<TEntity, TResult>
{
    public Expression<Func<TEntity, bool>>? Predicate { get; init; }
    public required Expression<Func<TEntity, TResult>> Selector { get; init; }
    public IReadOnlyList<SortRule<TEntity>> OrderBy { get; init; } = Array.Empty<SortRule<TEntity>>();
    public CursorPageRequest Page { get; init; } = new();
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; init; } = Array.Empty<Expression<Func<TEntity, object>>>();
    public bool IncludeDeleted { get; init; }
    public bool AsNoTracking { get; init; } = true;
    public string? Tag { get; init; }
    public string? ProtectionKey { get; init; }
    public int MaxLimit { get; init; } = 200;
}
