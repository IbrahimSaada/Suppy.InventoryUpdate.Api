using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Microsoft.Extensions.Options;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class DefaultCacheKeyFactory : ICacheKeyFactory
{
    private readonly RedisOptions _options;

    public DefaultCacheKeyFactory(IOptions<RedisOptions> options)
    {
        _options = options.Value;
    }

    public string Create(string category, params object?[] segments)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Cache key category is required.", nameof(category));
        }

        var prefix = Normalize(_options.InstancePrefix);
        var categoryPart = Normalize(category);
        var segmentParts = segments
            .Where(x => x is not null)
            .Select(x => Normalize(x!.ToString()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return segmentParts.Length == 0
            ? $"{prefix}:{categoryPart}"
            : $"{prefix}:{categoryPart}:{string.Join(':', segmentParts)}";
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Trim().Replace(':', '_');
    }
}
