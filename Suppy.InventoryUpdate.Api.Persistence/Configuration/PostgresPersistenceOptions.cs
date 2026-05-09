namespace Suppy.InventoryUpdate.Api.Persistence.Configuration;

public sealed class PostgresPersistenceOptions
{
    public string ConnectionStringName { get; set; } = "Postgres";
    public int? CommandTimeoutSeconds { get; set; }
    public bool EnableDetailedErrors { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
}
