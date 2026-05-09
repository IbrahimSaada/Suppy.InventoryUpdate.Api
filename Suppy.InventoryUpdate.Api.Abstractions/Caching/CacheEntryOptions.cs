namespace Suppy.InventoryUpdate.Api.Abstractions.Caching;

public sealed class CacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }
}
