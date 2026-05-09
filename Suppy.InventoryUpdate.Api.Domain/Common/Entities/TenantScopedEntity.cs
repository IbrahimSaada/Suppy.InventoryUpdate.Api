using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class TenantScopedEntity<TKey> : SoftDeletableEntity<TKey>, ITenantScoped
    where TKey : notnull
{
    protected TenantScopedEntity()
    {
    }

    protected TenantScopedEntity(TenantId tenantId)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    }

    public TenantId TenantId { get; protected set; } = null!;
}
