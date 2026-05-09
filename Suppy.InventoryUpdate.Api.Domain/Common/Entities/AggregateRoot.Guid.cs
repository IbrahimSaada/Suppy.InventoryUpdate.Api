namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot()
    {
        Id = Guid.NewGuid();
    }
}
