using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class TenantScopedAggregateRoot<TKey> : AggregateRoot<TKey>, ITenantScoped
    where TKey : notnull
{
    protected TenantScopedAggregateRoot()
    {
    }

    protected TenantScopedAggregateRoot(TenantId tenantId)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    }

    public TenantId TenantId { get; protected set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public void MarkDeleted(DateTime utcNow)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
