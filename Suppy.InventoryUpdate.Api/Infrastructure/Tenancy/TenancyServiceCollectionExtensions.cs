using Suppy.InventoryUpdate.Api.Abstractions.Tenancy;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Tenancy;

internal static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateTenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, HttpCurrentTenant>();

        return services;
    }
}
