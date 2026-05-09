using Suppy.InventoryUpdate.Api.Abstractions.Caching;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Caching;
using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;
using Microsoft.Extensions.Logging;

namespace Suppy.InventoryUpdate.Api.Application.Behaviors;

public sealed class QueryCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheStore _cacheStore;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        ICacheStore cacheStore,
        ICacheKeyFactory cacheKeyFactory,
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
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
        if (request is not ICacheableQuery<TResponse> cacheableQuery ||
            cacheableQuery.BypassCache)
        {
            return await next();
        }

        string? cacheKey = null;
        CachedQueryPayload<TResponse>? cached = null;
        try
        {
            cacheKey = _cacheKeyFactory.Create(cacheableQuery.CacheCategory, cacheableQuery.CacheKey);
            cached = await _cacheStore.GetAsync<CachedQueryPayload<TResponse>>(cacheKey, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Cache read failed for {RequestType} with key {CacheKey}. Continuing without cache.",
                typeof(TRequest).Name,
                cacheKey ?? "(unresolved)");
        }

        if (cached is not null)
        {
            return Result<TResponse>.Success(cached.Value);
        }

        var result = await next();
        if (result.IsFailure)
        {
            return result;
        }

        var cacheEntryOptions = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheableQuery.AbsoluteExpirationRelativeToNow
        };

        try
        {
            cacheKey ??= _cacheKeyFactory.Create(cacheableQuery.CacheCategory, cacheableQuery.CacheKey);
            await _cacheStore.SetAsync(
                cacheKey,
                new CachedQueryPayload<TResponse>(result.Value),
                cacheEntryOptions,
                ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Cache write failed for {RequestType} with key {CacheKey}. Response will continue.",
                typeof(TRequest).Name,
                cacheKey ?? "(unresolved)");
        }

        return result;
    }

    private sealed record CachedQueryPayload<T>(T Value);
}
