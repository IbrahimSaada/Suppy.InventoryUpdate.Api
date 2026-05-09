using Suppy.InventoryUpdate.Api.Abstractions.Observability;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Observability;

internal sealed class HttpCorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(CorrelationIdConstants.HttpContextItemKey, out var value) &&
                value is string correlationId &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return httpContext.TraceIdentifier;
        }
    }
}
