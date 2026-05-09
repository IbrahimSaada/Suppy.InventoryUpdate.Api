using System.Diagnostics;
using System.Threading.RateLimiting;
using Suppy.InventoryUpdate.Api.Presentation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Suppy.InventoryUpdate.Api.Infrastructure.RateLimiting;

internal static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddTenantRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TenantRateLimitingOptions>()
            .Bind(configuration.GetSection(TenantRateLimitingOptions.SectionName))
            .Validate(options => options.PermitLimit > 0, "RateLimiting:Tenant:PermitLimit must be greater than zero.")
            .Validate(options => options.WindowSeconds > 0, "RateLimiting:Tenant:WindowSeconds must be greater than zero.")
            .Validate(options => options.QueueLimit >= 0, "RateLimiting:Tenant:QueueLimit cannot be negative.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                var httpContext = context.HttpContext;
                var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
                }

                var payload = ApiEnvelope<ApiErrorPayload>.Failure(
                    "Too many requests for this tenant. Retry later.",
                    new ApiErrorPayload("rate_limit.exceeded", StatusCodes.Status429TooManyRequests, traceId));

                await httpContext.Response.WriteAsJsonAsync(payload, ct);
            };

            options.AddPolicy(TenantRateLimitingPolicyNames.Tenant, httpContext =>
            {
                var rateOptions = httpContext.RequestServices
                    .GetRequiredService<IOptions<TenantRateLimitingOptions>>()
                    .Value;

                var partitionKey = TenantPartitionKeyResolver.Resolve(httpContext, rateOptions);
                if (!rateOptions.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter(partitionKey);
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateOptions.PermitLimit,
                        QueueLimit = rateOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        Window = TimeSpan.FromSeconds(rateOptions.WindowSeconds)
                    });
            });
        });

        return services;
    }
}
