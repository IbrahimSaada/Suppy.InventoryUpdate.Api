using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.GenericRepo.Ef;
using Suppy.InventoryUpdate.Api.GenericRepo.Mongo;
using Suppy.InventoryUpdate.Api.Persistence.Configuration;
using Suppy.InventoryUpdate.Api.Persistence.Outbox;
using Suppy.InventoryUpdate.Api.Persistence.Products;
using Suppy.InventoryUpdate.Api.Persistence.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Suppy.InventoryUpdate.Api.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SqlPersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new SqlPersistenceOptions();
        configure?.Invoke(options);

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing connection string '{options.ConnectionStringName}'. Configure it under ConnectionStrings.");
        }

        services.AddDbContext<AppDbContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);

                if (options.CommandTimeoutSeconds is > 0)
                {
                    sqlServerOptions.CommandTimeout(options.CommandTimeoutSeconds.Value);
                }
            });

            if (options.EnableDetailedErrors)
            {
                dbOptions.EnableDetailedErrors();
            }

            if (options.EnableSensitiveDataLogging)
            {
                dbOptions.EnableSensitiveDataLogging();
            }
        });

        services.AddGenericEfRepositories<AppDbContext>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<IProductBatchProcessingStore, EfProductBatchProcessingStore>();
        services.AddScoped<IProductReadStore, EfProductReadStore>();

        return services;
    }

    public static IServiceCollection AddPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<PostgresPersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new PostgresPersistenceOptions();
        configure?.Invoke(options);

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing connection string '{options.ConnectionStringName}'. Configure it under ConnectionStrings.");
        }

        services.AddDbContext<AppDbContext>(dbOptions =>
        {
            dbOptions.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);

                if (options.CommandTimeoutSeconds is > 0)
                {
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds.Value);
                }
            });

            if (options.EnableDetailedErrors)
            {
                dbOptions.EnableDetailedErrors();
            }

            if (options.EnableSensitiveDataLogging)
            {
                dbOptions.EnableSensitiveDataLogging();
            }
        });

        services.AddGenericEfRepositories<AppDbContext>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<IProductBatchProcessingStore, EfProductBatchProcessingStore>();
        services.AddScoped<IProductReadStore, EfProductReadStore>();

        return services;
    }

    public static IServiceCollection AddMongoPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MongoPersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new MongoPersistenceOptions();
        configure?.Invoke(options);

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing Mongo connection string '{options.ConnectionStringName}'. Configure it under ConnectionStrings.");
        }

        var databaseName = configuration[options.DatabaseNameKey];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                $"Missing Mongo database name at key '{options.DatabaseNameKey}'.");
        }

        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(mongoDatabase);
        services.AddGenericMongoRepositories(mongoDatabase);

        return services;
    }
}
