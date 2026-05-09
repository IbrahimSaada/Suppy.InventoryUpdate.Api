using Suppy.InventoryUpdate.Api.Abstractions.Tenancy;
using DomainTenantId = Suppy.InventoryUpdate.Api.Domain.Tenancy.TenantId;

namespace Suppy.InventoryUpdate.Api.Infrastructure.RateLimiting;

internal static class TenantPartitionKeyResolver
{
    public static string Resolve(HttpContext httpContext, TenantRateLimitingOptions options)
    {
        var headerTenant = ResolveHeaderTenant(httpContext);
        if (headerTenant is not null)
        {
            return $"tenant:{headerTenant}";
        }

        var claimTenant = ResolveClaimTenant(httpContext);
        if (claimTenant is not null)
        {
            return $"tenant:{claimTenant}";
        }

        if (options.IncludeQueryStringTenantId)
        {
            var queryTenant = ResolveQueryTenant(httpContext);
            if (queryTenant is not null)
            {
                return $"tenant:{queryTenant}";
            }
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static string? ResolveHeaderTenant(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(TenantConstants.HeaderName, out var values))
        {
            return null;
        }

        return Normalize(values.FirstOrDefault());
    }

    private static string? ResolveClaimTenant(HttpContext httpContext)
    {
        return Normalize(httpContext.User.FindFirst(TenantConstants.ClaimType)?.Value);
    }

    private static string? ResolveQueryTenant(HttpContext httpContext)
    {
        return Normalize(httpContext.Request.Query["tenantId"].FirstOrDefault());
    }

    private static string? Normalize(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        return DomainTenantId.TryCreate(tenantId, out var parsedTenantId) && parsedTenantId is not null
            ? parsedTenantId.Value
            : null;
    }
}
