using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration[$"{MessagingOptions.SectionName}:Provider"] ?? "None";
        ValidateProviderCombination(configuration, provider);

        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Validate(
                options =>
                    string.Equals(options.Provider, "None", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(options.Provider, "RabbitMq", StringComparison.OrdinalIgnoreCase),
                "Messaging:Provider must be one of: None, RabbitMq.")
            .ValidateOnStart();

        services.AddOptions<OutboxDispatcherOptions>()
            .Bind(configuration.GetSection(OutboxDispatcherOptions.SectionName))
            .Validate(options => options.BatchSize > 0, "Messaging:Outbox:BatchSize must be greater than zero.")
            .Validate(options => options.PollIntervalSeconds > 0, "Messaging:Outbox:PollIntervalSeconds must be greater than zero.")
            .Validate(options => options.LockTimeoutSeconds > 0, "Messaging:Outbox:LockTimeoutSeconds must be greater than zero.")
            .Validate(options => options.MaxRetryCount > 0, "Messaging:Outbox:MaxRetryCount must be greater than zero.")
            .Validate(options => options.BaseBackoffSeconds > 0, "Messaging:Outbox:BaseBackoffSeconds must be greater than zero.")
            .Validate(options => options.MaxErrorLength > 0, "Messaging:Outbox:MaxErrorLength must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(
                options =>
                    !string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(options.HostName),
                "Messaging:RabbitMq:HostName is required when Messaging:Provider=RabbitMq.")
            .Validate(
                options =>
                    !string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase) ||
                    options.Port > 0,
                "Messaging:RabbitMq:Port must be greater than zero when Messaging:Provider=RabbitMq.")
            .Validate(
                options =>
                    !string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(options.ExchangeName),
                "Messaging:RabbitMq:ExchangeName is required when Messaging:Provider=RabbitMq.")
            .Validate(
                options =>
                    !string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase) ||
                    !options.ConsumerEnabled ||
                    options.ConsumerPrefetchCount == 1,
                "Messaging:RabbitMq:ConsumerPrefetchCount must be 1 when consumer is enabled.")
            .Validate(
                options =>
                    !string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase) ||
                    !options.PublisherConfirmsEnabled ||
                    options.PublisherConfirmTimeoutSeconds > 0,
                "Messaging:RabbitMq:PublisherConfirmTimeoutSeconds must be greater than zero when publisher confirms are enabled.")
            .ValidateOnStart();

        services.TryAddScoped<IOutboxStore, NoOpOutboxStore>();
        services.AddSingleton<IOutboxSerializer, SystemTextJsonOutboxSerializer>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        if (string.Equals(provider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        }
        else
        {
            services.AddSingleton<IMessageBus, NoOpMessageBus>();
        }

        services.AddHostedService<OutboxDispatcherHostedService>();
        services.AddHostedService<RabbitMqConsumerHostedService>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "ready" });

        return services;
    }

    private static void ValidateProviderCombination(IConfiguration configuration, string messagingProvider)
    {
        var outboxEnabled = configuration.GetValue<bool>($"{OutboxDispatcherOptions.SectionName}:Enabled");
        var consumerEnabled = configuration.GetValue<bool>($"{RabbitMqOptions.SectionName}:ConsumerEnabled");
        var persistenceProvider = configuration["Persistence:Provider"] ?? "None";

        if (consumerEnabled && !string.Equals(messagingProvider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Messaging:RabbitMq:ConsumerEnabled=true requires Messaging:Provider=RabbitMq.");
        }

        if (!string.Equals(messagingProvider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!outboxEnabled)
        {
            throw new InvalidOperationException(
                "Messaging:Provider=RabbitMq requires Messaging:Outbox:Enabled=true.");
        }

        var supportsDurableOutbox =
            string.Equals(persistenceProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(persistenceProvider, "Postgres", StringComparison.OrdinalIgnoreCase);

        if (!supportsDurableOutbox)
        {
            throw new InvalidOperationException(
                $"Messaging:Provider=RabbitMq requires durable SQL outbox support, " +
                $"but Persistence:Provider is '{persistenceProvider}'. Use SqlServer or Postgres.");
        }
    }
}
