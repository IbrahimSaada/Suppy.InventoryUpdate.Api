using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Caching;
using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;
using Microsoft.Extensions.Logging;

namespace Suppy.InventoryUpdate.Api.Application.Behaviors;

public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheStore _cacheStore;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(
        ICacheStore cacheStore,
        ICacheKeyFactory cacheKeyFactory,
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cacheStore = cacheStore;
        _cacheKeyFactory = cacheKeyFactory;
        _logger = logger;
    }

    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        var result = await next();

        if (result.IsFailure ||
            request is not ICommand<TResponse> ||
            request is not IInvalidatesCache invalidatingRequest)
        {
            return result;
        }

        if (invalidatingRequest.CacheInvalidationItems.Count == 0)
        {
            return result;
        }

        var distinctItems = invalidatingRequest.CacheInvalidationItems
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Category) &&
                !string.IsNullOrWhiteSpace(item.Key))
            .Distinct()
            .ToArray();

        foreach (var item in distinctItems)
        {
            string? cacheKey = null;
            try
            {
                cacheKey = _cacheKeyFactory.Create(item.Category, item.Key);
                await _cacheStore.RemoveAsync(cacheKey, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Cache invalidation failed for {RequestType} with key {CacheKey}. Command result remains successful.",
                    typeof(TRequest).Name,
                    cacheKey ?? "(unresolved)");
            }
        }

        return result;
    }
}
