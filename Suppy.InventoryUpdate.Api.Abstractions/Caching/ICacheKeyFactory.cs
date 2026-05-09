namespace Suppy.InventoryUpdate.Api.Abstractions.Caching;

public interface ICacheKeyFactory
{
    string Create(string category, params object?[] segments);
}
