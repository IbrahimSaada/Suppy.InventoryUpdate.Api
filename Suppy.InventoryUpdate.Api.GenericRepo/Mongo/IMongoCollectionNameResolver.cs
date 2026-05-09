namespace Suppy.InventoryUpdate.Api.GenericRepo.Mongo;

public interface IMongoCollectionNameResolver
{
    string Resolve<TEntity>();
}
