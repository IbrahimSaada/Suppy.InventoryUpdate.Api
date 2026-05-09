using System.Collections.Concurrent;
using System.Reflection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class PropertyCache
{
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> Cache = new();

    public static PropertyInfo? GetProperty(Type type, string propertyName)
    {
        return Cache.GetOrAdd(
            (type, propertyName),
            key => key.Type.GetProperty(
                key.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy));
    }
}
