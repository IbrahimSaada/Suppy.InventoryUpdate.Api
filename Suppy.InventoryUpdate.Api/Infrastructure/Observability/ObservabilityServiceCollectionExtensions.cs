using Suppy.InventoryUpdate.Api.Abstractions.Observability;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Observability;

internal static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateObservability(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationContext, HttpCorrelationContext>();

        return services;
    }
}
