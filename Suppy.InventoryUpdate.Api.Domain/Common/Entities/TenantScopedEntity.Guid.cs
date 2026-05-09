using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class TenantScopedEntity : TenantScopedEntity<Guid>
{
    protected TenantScopedEntity()
    {
        Id = Guid.NewGuid();
    }

    protected TenantScopedEntity(TenantId tenantId)
        : base(tenantId)
    {
        Id = Guid.NewGuid();
    }
}
