namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class SoftDeletableEntity : SoftDeletableEntity<Guid>
{
    protected SoftDeletableEntity()
    {
        Id = Guid.NewGuid();
    }
}
