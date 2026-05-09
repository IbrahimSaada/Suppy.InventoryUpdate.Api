namespace Suppy.InventoryUpdate.Api.Domain.Tenancy;

public interface ITenantScoped
{
    TenantId TenantId { get; }
}
