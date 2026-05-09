using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Contracts.Caching;

public interface ICacheableQuery<TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }

    string CacheCategory => CacheCategories.Query;

    TimeSpan? AbsoluteExpirationRelativeToNow => null;

    bool BypassCache => false;
}
