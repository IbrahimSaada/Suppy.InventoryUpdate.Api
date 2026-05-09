using System.Security.Claims;
using Suppy.InventoryUpdate.Api.Abstractions.Tenancy;
using DomainTenantId = Suppy.InventoryUpdate.Api.Domain.Tenancy.TenantId;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Tenancy;

internal sealed class HttpCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsResolved => TenantId is not null;

    public string? TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            var headerTenant = ResolveFromHeader(httpContext);
            if (headerTenant is not null)
            {
                return headerTenant;
            }

            return ResolveFromClaims(httpContext.User);
        }
    }

    private static string? ResolveFromHeader(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(TenantConstants.HeaderName, out var values))
        {
            return null;
        }

        return Normalize(values.FirstOrDefault());
    }

    private static string? ResolveFromClaims(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(TenantConstants.ClaimType);
        return Normalize(claim?.Value);
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
