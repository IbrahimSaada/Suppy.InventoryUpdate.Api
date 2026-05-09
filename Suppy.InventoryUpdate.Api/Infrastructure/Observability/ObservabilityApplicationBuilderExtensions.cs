namespace Suppy.InventoryUpdate.Api.Infrastructure.Observability;

internal static class ObservabilityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTemplateCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
