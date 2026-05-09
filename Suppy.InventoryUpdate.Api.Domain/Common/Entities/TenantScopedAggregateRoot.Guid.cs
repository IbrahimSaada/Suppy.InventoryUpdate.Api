using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class TenantScopedAggregateRoot : TenantScopedAggregateRoot<Guid>
{
    protected TenantScopedAggregateRoot()
    {
        Id = Guid.NewGuid();
    }

    protected TenantScopedAggregateRoot(TenantId tenantId)
        : base(tenantId)
    {
        Id = Guid.NewGuid();
    }
}
