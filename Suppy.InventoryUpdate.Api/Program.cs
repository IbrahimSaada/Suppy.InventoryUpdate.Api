using Suppy.InventoryUpdate.Api.Abstractions.Clock;
using Suppy.InventoryUpdate.Api.Abstractions.Persistence;
using Suppy.InventoryUpdate.Api.Application;
using Suppy.InventoryUpdate.Api.Infrastructure;
using Suppy.InventoryUpdate.Api.Infrastructure.Caching;
using Suppy.InventoryUpdate.Api.Infrastructure.Messaging;
using Suppy.InventoryUpdate.Api.Infrastructure.Observability;
using Suppy.InventoryUpdate.Api.Infrastructure.Products;
using Suppy.InventoryUpdate.Api.Infrastructure.Tenancy;
using Suppy.InventoryUpdate.Api.Persistence;
using Suppy.InventoryUpdate.Api.Security.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Suppy Inventory Update API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddTemplateObservability();
builder.Services.AddTemplateTenancy();
builder.Services.AddTemplateSecurity(builder.Configuration);
builder.Services.AddTemplateCaching(builder.Configuration);
builder.Services.AddTemplateMessaging(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck(
    "self",
    () => HealthCheckResult.Healthy("Process is alive."),
    tags: new[] { "live" });

// Fallback infrastructure so template runs even when persistence provider is not configured yet.
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

ConfigurePersistence(builder);
ConfigureProductBatchProcessing(builder);

var app = builder.Build();

app.UseGlobalExceptionHandling(app.Environment.IsDevelopment());
app.UseTemplateCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Suppy Inventory Update API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });
app.MapControllers();

app.Run();

static void ConfigurePersistence(WebApplicationBuilder builder)
{
    var provider = builder.Configuration["Persistence:Provider"] ?? "None";

    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddSqlServerPersistence(builder.Configuration);
        return;
    }

    if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddPostgresPersistence(builder.Configuration);
        return;
    }

    if (provider.Equals("Mongo", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddMongoPersistence(builder.Configuration);
    }
}

static void ConfigureProductBatchProcessing(WebApplicationBuilder builder)
{
    builder.Services.AddOptions<ProductBatchProcessingOptions>()
        .Bind(builder.Configuration.GetSection(ProductBatchProcessingOptions.SectionName))
        .Validate(options => options.BatchSize > 0, "ProductBatchProcessing:BatchSize must be greater than zero.")
        .Validate(options => options.PollIntervalSeconds > 0, "ProductBatchProcessing:PollIntervalSeconds must be greater than zero.")
        .ValidateOnStart();

    builder.Services.AddHostedService<ProductBatchProcessingWorker>();
}

public partial class Program;
