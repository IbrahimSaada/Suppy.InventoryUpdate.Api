using System.Linq.Expressions;

namespace Suppy.InventoryUpdate.Api.Abstractions.Paging;

public sealed record SortRule<TEntity>(
    Expression<Func<TEntity, object>> KeySelector,
    SortDirection Direction)
{
    public static SortRule<TEntity> Asc(Expression<Func<TEntity, object>> keySelector)
    {
        return new SortRule<TEntity>(keySelector, SortDirection.Asc);
    }

    public static SortRule<TEntity> Desc(Expression<Func<TEntity, object>> keySelector)
    {
        return new SortRule<TEntity>(keySelector, SortDirection.Desc);
    }
}
