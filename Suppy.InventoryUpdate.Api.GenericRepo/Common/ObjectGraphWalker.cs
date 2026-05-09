using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class ObjectGraphWalker
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> NavigationPropertiesCache = new();

    public static void Traverse(object root, Action<object> visitor)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(visitor);

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<object>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            visitor(current);

            var properties = NavigationPropertiesCache.GetOrAdd(current.GetType(), GetNavigationProperties);
            foreach (var property in properties)
            {
                var value = property.GetValue(current);
                if (value is null)
                {
                    continue;
                }

                if (value is string)
                {
                    continue;
                }

                if (value is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is null || item is string)
                        {
                            continue;
                        }

                        if (item.GetType().IsValueType)
                        {
                            continue;
                        }

                        stack.Push(item);
                    }

                    continue;
                }

                if (!value.GetType().IsValueType)
                {
                    stack.Push(value);
                }
            }
        }
    }

    private static PropertyInfo[] GetNavigationProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead &&
                property.GetIndexParameters().Length == 0 &&
                property.PropertyType != typeof(string) &&
                !property.PropertyType.IsValueType)
            .ToArray();
    }
}
