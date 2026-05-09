namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class Entity : Entity<Guid>
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }
}
