using System.Linq.Expressions;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class EntityMetadata<TEntity>
{
    private const string IsDeletedName = "IsDeleted";
    private const string DeletedAtUtcName = "DeletedAtUtc";
    private const string CreatedAtUtcName = "CreatedAtUtc";
    private const string UpdatedAtUtcName = "UpdatedAtUtc";

    public static readonly Type? IsDeletedType = PropertyCache.GetProperty(typeof(TEntity), IsDeletedName)?.PropertyType;
    public static readonly Type? DeletedAtUtcType = PropertyCache.GetProperty(typeof(TEntity), DeletedAtUtcName)?.PropertyType;
    public static readonly Type? CreatedAtUtcType = PropertyCache.GetProperty(typeof(TEntity), CreatedAtUtcName)?.PropertyType;
    public static readonly Type? UpdatedAtUtcType = PropertyCache.GetProperty(typeof(TEntity), UpdatedAtUtcName)?.PropertyType;

    public static readonly bool SupportsSoftDelete =
        IsDeletedType == typeof(bool) &&
        DeletedAtUtcType == typeof(DateTime?);

    public static readonly bool HasCreatedAtUtc =
        CreatedAtUtcType is not null &&
        (CreatedAtUtcType == typeof(DateTime) || CreatedAtUtcType == typeof(DateTime?));

    public static readonly bool HasUpdatedAtUtc =
        UpdatedAtUtcType is not null &&
        (UpdatedAtUtcType == typeof(DateTime) || UpdatedAtUtcType == typeof(DateTime?));

    public static readonly Expression<Func<TEntity, bool>>? NotDeletedPredicate = BuildNotDeletedPredicate();

    private static Expression<Func<TEntity, bool>>? BuildNotDeletedPredicate()
    {
        if (!SupportsSoftDelete)
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var isDeletedProperty = Expression.Property(parameter, IsDeletedName);
        var expression = Expression.Not(isDeletedProperty);

        return Expression.Lambda<Func<TEntity, bool>>(expression, parameter);
    }
}
