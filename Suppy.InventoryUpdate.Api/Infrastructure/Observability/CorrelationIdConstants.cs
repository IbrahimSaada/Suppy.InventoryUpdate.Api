namespace Suppy.InventoryUpdate.Api.Infrastructure.Observability;

internal static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-Id";

    public const string HttpContextItemKey = "__template_correlation_id";
}
