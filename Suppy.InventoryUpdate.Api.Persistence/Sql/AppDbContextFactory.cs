using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Suppy.InventoryUpdate.Api.Persistence.Sql;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        });

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString(string[] args)
    {
        var argumentConnection =
            TryGetArgValue(args, "--connection") ??
            TryGetPrefixedArgValue(args, "--connection=");

        if (!string.IsNullOrWhiteSpace(argumentConnection))
        {
            return argumentConnection;
        }

        var envConnection =
            Environment.GetEnvironmentVariable("BACKEND_TEMPLATE_MIGRATIONS_CONNECTION") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            return envConnection;
        }

        throw new InvalidOperationException(
            "No design-time connection string configured. " +
            "Set BACKEND_TEMPLATE_MIGRATIONS_CONNECTION (or ConnectionStrings__Postgres) " +
            "or pass --connection=\"...\" to dotnet ef.");
    }

    private static string? TryGetArgValue(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string? TryGetPrefixedArgValue(string[] args, string prefix)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }
        }

        return null;
    }
}

