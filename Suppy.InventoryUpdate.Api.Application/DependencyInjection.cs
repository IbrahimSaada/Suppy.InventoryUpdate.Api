using System.Reflection;
using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Application.Behaviors;
using Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;
using Suppy.InventoryUpdate.Api.Application.Dispatching;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Suppy.InventoryUpdate.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(DependencyInjection).Assembly;

        RegisterClosedGenericImplementations(services, assembly, typeof(IRequestHandler<,>));
        RegisterClosedGenericImplementations(services, assembly, typeof(IIntegrationEventConsumer<>));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }

    private static void RegisterClosedGenericImplementations(
        IServiceCollection services,
        Assembly assembly,
        Type openGenericServiceType)
    {
        var candidates = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && !type.IsGenericTypeDefinition)
            .ToArray();

        foreach (var implementationType in candidates)
        {
            var serviceTypes = implementationType
                .GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == openGenericServiceType)
                .ToArray();

            foreach (var serviceType in serviceTypes)
            {
                services.AddScoped(serviceType, implementationType);
            }
        }
    }
}
