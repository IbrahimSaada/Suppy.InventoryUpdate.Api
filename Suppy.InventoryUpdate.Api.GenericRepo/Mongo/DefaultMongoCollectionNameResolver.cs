using System.Reflection;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Mongo;

public sealed class DefaultMongoCollectionNameResolver : IMongoCollectionNameResolver
{
    public string Resolve<TEntity>()
    {
        var attribute = typeof(TEntity).GetCustomAttribute<MongoCollectionAttribute>();
        return attribute?.Name ?? typeof(TEntity).Name;
    }
}
