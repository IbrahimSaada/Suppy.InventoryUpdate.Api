namespace Suppy.InventoryUpdate.Api.Persistence.Configuration;

public sealed class MongoPersistenceOptions
{
    public string ConnectionStringName { get; set; } = "Mongo";
    public string DatabaseNameKey { get; set; } = "Mongo:Database";
}
