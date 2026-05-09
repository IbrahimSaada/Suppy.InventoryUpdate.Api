namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string InstancePrefix { get; set; } = "backend.foundation.template";

    public int DefaultTtlSeconds { get; set; } = 300;

    public int ConnectTimeoutMs { get; set; } = 5000;
}
