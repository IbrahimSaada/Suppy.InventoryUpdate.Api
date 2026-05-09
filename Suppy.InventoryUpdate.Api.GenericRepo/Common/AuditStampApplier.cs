using System.Reflection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class AuditStampApplier
{
    private const string CreatedAtUtc = "CreatedAtUtc";
    private const string UpdatedAtUtc = "UpdatedAtUtc";
    private const string IsDeleted = "IsDeleted";
    private const string DeletedAtUtc = "DeletedAtUtc";

    public static void ApplyCreationStamps(object root, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(root);

        ObjectGraphWalker.Traverse(root, node =>
        {
            TrySetCreatedAtUtc(node, utcNow);
            TryClearUpdatedAtUtc(node);
        });
    }

    public static void TouchUpdatedAt(object entity, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var updatedProperty = PropertyCache.GetProperty(entity.GetType(), UpdatedAtUtc);
        if (updatedProperty is null || !updatedProperty.CanWrite)
        {
            return;
        }

        if (updatedProperty.PropertyType == typeof(DateTime?))
        {
            updatedProperty.SetValue(entity, utcNow);
            return;
        }

        if (updatedProperty.PropertyType == typeof(DateTime))
        {
            updatedProperty.SetValue(entity, utcNow);
        }
    }

    public static bool TryMarkDeleted(object entity, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var isDeletedProperty = PropertyCache.GetProperty(entity.GetType(), IsDeleted);
        var deletedAtProperty = PropertyCache.GetProperty(entity.GetType(), DeletedAtUtc);
        if (isDeletedProperty is null || deletedAtProperty is null)
        {
            return false;
        }

        if (isDeletedProperty.PropertyType != typeof(bool) || !isDeletedProperty.CanWrite)
        {
            return false;
        }

        if (deletedAtProperty.PropertyType != typeof(DateTime?) || !deletedAtProperty.CanWrite)
        {
            return false;
        }

        var isDeleted = (bool)(isDeletedProperty.GetValue(entity) ?? false);
        if (isDeleted)
        {
            return false;
        }

        isDeletedProperty.SetValue(entity, true);
        deletedAtProperty.SetValue(entity, utcNow);
        TouchUpdatedAt(entity, utcNow);

        return true;
    }

    private static void TrySetCreatedAtUtc(object node, DateTime utcNow)
    {
        var createdProperty = PropertyCache.GetProperty(node.GetType(), CreatedAtUtc);
        if (createdProperty is null || !createdProperty.CanWrite)
        {
            return;
        }

        if (createdProperty.PropertyType == typeof(DateTime))
        {
            var current = (DateTime?)createdProperty.GetValue(node) ?? default;
            if (current == default)
            {
                createdProperty.SetValue(node, utcNow);
            }

            return;
        }

        if (createdProperty.PropertyType == typeof(DateTime?))
        {
            var current = (DateTime?)createdProperty.GetValue(node);
            if (!current.HasValue || current.Value == default)
            {
                createdProperty.SetValue(node, utcNow);
            }
        }
    }

    private static void TryClearUpdatedAtUtc(object node)
    {
        var updatedProperty = PropertyCache.GetProperty(node.GetType(), UpdatedAtUtc);
        if (updatedProperty is null || !updatedProperty.CanWrite)
        {
            return;
        }

        if (updatedProperty.PropertyType == typeof(DateTime?))
        {
            updatedProperty.SetValue(node, null);
        }
    }
}
