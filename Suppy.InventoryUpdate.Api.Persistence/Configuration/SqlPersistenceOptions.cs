namespace Suppy.InventoryUpdate.Api.Persistence.Configuration;

public sealed class SqlPersistenceOptions
{
    public string ConnectionStringName { get; set; } = "DefaultConnection";
    public int? CommandTimeoutSeconds { get; set; }
    public bool EnableDetailedErrors { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
}
