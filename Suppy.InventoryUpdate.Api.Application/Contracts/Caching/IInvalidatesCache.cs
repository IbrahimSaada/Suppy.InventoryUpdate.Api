namespace Suppy.InventoryUpdate.Api.Application.Contracts.Caching;

public interface IInvalidatesCache
{
    IReadOnlyCollection<CacheInvalidationItem> CacheInvalidationItems { get; }
}

public sealed record CacheInvalidationItem(string Category, string Key);
