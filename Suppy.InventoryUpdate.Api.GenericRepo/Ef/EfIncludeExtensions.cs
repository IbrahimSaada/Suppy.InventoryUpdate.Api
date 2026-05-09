using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Ef;

internal static class EfIncludeExtensions
{
    public static IQueryable<TEntity> ApplyIncludes<TEntity>(
        this IQueryable<TEntity> query,
        IReadOnlyList<Expression<Func<TEntity, object>>>? includes)
        where TEntity : class
    {
        if (includes is null || includes.Count == 0)
        {
            return query;
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query;
    }
}
