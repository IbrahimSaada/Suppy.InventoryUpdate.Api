namespace Suppy.InventoryUpdate.Api.GenericRepo.Mongo;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MongoCollectionAttribute : Attribute
{
    public MongoCollectionAttribute(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Collection name cannot be empty.", nameof(name))
            : name;
    }

    public string Name { get; }
}
