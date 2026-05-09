using Suppy.InventoryUpdate.Api.Abstractions.Security;
using Suppy.InventoryUpdate.Api.Security.Authentication;
using Suppy.InventoryUpdate.Api.Security.Authorization;
using Suppy.InventoryUpdate.Api.Security.Configuration;
using Suppy.InventoryUpdate.Api.Security.CurrentUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TemplateAuthenticationOptions = Suppy.InventoryUpdate.Api.Security.Configuration.AuthenticationOptions;

namespace Suppy.InventoryUpdate.Api.Security.DependencyInjection;

internal static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TemplateAuthenticationOptions>()
            .Bind(configuration.GetSection(TemplateAuthenticationOptions.SectionName))
            .Validate(
                options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Authority),
                "Authentication:Authority is required when Authentication:Enabled=true.")
            .Validate(
                options =>
                    !options.Enabled ||
                    !options.ValidateAudience ||
                    options.Audiences.Any(x => !string.IsNullOrWhiteSpace(x)),
                "Authentication:Audiences must include at least one value when audience validation is enabled.")
            .Validate(
                options => options.ClockSkewSeconds >= 0,
                "Authentication:ClockSkewSeconds must be >= 0.")
            .ValidateOnStart();

        services.AddOptions<AuthorizationMappingOptions>()
            .Bind(configuration.GetSection(AuthorizationMappingOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeycloakRealmAccessClaimType),
                "AuthorizationMapping:KeycloakRealmAccessClaimType is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeycloakResourceAccessClaimType),
                "AuthorizationMapping:KeycloakResourceAccessClaimType is required.")
            .Validate(
                options => options.RoleClaimTypes.Count > 0,
                "AuthorizationMapping:RoleClaimTypes must contain at least one value.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        var authOptions = configuration
            .GetSection(TemplateAuthenticationOptions.SectionName)
            .Get<TemplateAuthenticationOptions>() ?? new TemplateAuthenticationOptions();

        if (!authOptions.Enabled)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = NoAuthAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = NoAuthAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, NoAuthAuthenticationHandler>(
                    NoAuthAuthenticationHandler.SchemeName,
                    _ => { });

            services.AddAuthorization();
            return services;
        }

        ValidateAuthenticationOptions(authOptions);

        var authenticationBuilder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        services.AddAuthorization();

        authenticationBuilder
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = authOptions.ValidateIssuer,
                    ValidateAudience = authOptions.ValidateAudience,
                    ValidateLifetime = authOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = authOptions.NameClaimType,
                    RoleClaimType = authOptions.RoleClaimType,
                    ClockSkew = TimeSpan.FromSeconds(Math.Max(0, authOptions.ClockSkewSeconds))
                };

                var audiences = authOptions.Audiences
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (audiences.Length > 0)
                {
                    options.TokenValidationParameters.ValidAudiences = audiences;
                }

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed.");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogDebug(
                            "JWT token validated for subject {Subject}.",
                            context.Principal?.FindFirst("sub")?.Value ?? "(unknown)");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogDebug("JWT challenge executed. Error: {Error}", context.Error);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static void ValidateAuthenticationOptions(TemplateAuthenticationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new InvalidOperationException(
                "Authentication is enabled but Authority is missing. Configure Authentication:Authority.");
        }

        if (options.ValidateAudience &&
            (options.Audiences is null || options.Audiences.Count == 0))
        {
            throw new InvalidOperationException(
                "Authentication audience validation is enabled, but no audiences are configured. Set Authentication:Audiences.");
        }

        if (options.ClockSkewSeconds < 0)
        {
            throw new InvalidOperationException("Authentication:ClockSkewSeconds must be >= 0.");
        }
    }
}
