using System.Linq.Expressions;
using System.Reflection;
using Suppy.InventoryUpdate.Api.Abstractions.Paging;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class QueryableOrderingExtensions
{
    private static readonly MethodInfo OrderByMethod = ResolveQueryableMethod(nameof(Queryable.OrderBy));
    private static readonly MethodInfo OrderByDescendingMethod = ResolveQueryableMethod(nameof(Queryable.OrderByDescending));
    private static readonly MethodInfo ThenByMethod = ResolveQueryableMethod(nameof(Queryable.ThenBy));
    private static readonly MethodInfo ThenByDescendingMethod = ResolveQueryableMethod(nameof(Queryable.ThenByDescending));

    public static IOrderedQueryable<TEntity> ApplyOrdering<TEntity, TKey>(
        this IQueryable<TEntity> query,
        IReadOnlyList<SortRule<TEntity>> rules,
        Expression<Func<TEntity, TKey>> stableKeySelector,
        bool useCreatedAtDefault)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(stableKeySelector);

        if (rules.Count == 0)
        {
            if (useCreatedAtDefault)
            {
                var createdAtSelector = BuildPropertySelector<TEntity>("CreatedAtUtc");
                var ordered = OrderBy(query, createdAtSelector, SortDirection.Desc);
                return ordered.ThenByDescending(stableKeySelector);
            }

            return query.OrderByDescending(stableKeySelector);
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;

        foreach (var rule in rules)
        {
            var normalizedSelector = StripObjectConversion(rule.KeySelector);
            orderedQuery = orderedQuery is null
                ? OrderBy(query, normalizedSelector, rule.Direction)
                : ThenBy(orderedQuery, normalizedSelector, rule.Direction);
        }

        if (!ContainsIdOrdering(rules))
        {
            orderedQuery = rules[0].Direction == SortDirection.Asc
                ? orderedQuery!.ThenBy(stableKeySelector)
                : orderedQuery!.ThenByDescending(stableKeySelector);
        }

        return orderedQuery!;
    }

    private static IOrderedQueryable<TEntity> OrderBy<TEntity>(
        IQueryable<TEntity> query,
        LambdaExpression keySelector,
        SortDirection direction)
    {
        var method = direction == SortDirection.Asc ? OrderByMethod : OrderByDescendingMethod;
        return InvokeOrderMethod<TEntity>(method, query, keySelector);
    }

    private static IOrderedQueryable<TEntity> ThenBy<TEntity>(
        IOrderedQueryable<TEntity> query,
        LambdaExpression keySelector,
        SortDirection direction)
    {
        var method = direction == SortDirection.Asc ? ThenByMethod : ThenByDescendingMethod;
        return InvokeOrderMethod<TEntity>(method, query, keySelector);
    }

    private static IOrderedQueryable<TEntity> InvokeOrderMethod<TEntity>(
        MethodInfo methodDefinition,
        object query,
        LambdaExpression keySelector)
    {
        var method = methodDefinition.MakeGenericMethod(typeof(TEntity), keySelector.ReturnType);
        return (IOrderedQueryable<TEntity>)method.Invoke(null, new object[] { query, keySelector })!;
    }

    private static LambdaExpression StripObjectConversion<TEntity>(Expression<Func<TEntity, object>> selector)
    {
        var body = selector.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        return Expression.Lambda(body, selector.Parameters);
    }

    private static bool ContainsIdOrdering<TEntity>(IReadOnlyList<SortRule<TEntity>> rules)
    {
        foreach (var rule in rules)
        {
            var body = rule.KeySelector.Body;
            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            if (body is MemberExpression member && string.Equals(member.Member.Name, "Id", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static LambdaExpression BuildPropertySelector<TEntity>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var body = Expression.Property(parameter, propertyName);
        return Expression.Lambda(body, parameter);
    }

    private static MethodInfo ResolveQueryableMethod(string methodName)
    {
        return typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == methodName &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 2);
    }
}
