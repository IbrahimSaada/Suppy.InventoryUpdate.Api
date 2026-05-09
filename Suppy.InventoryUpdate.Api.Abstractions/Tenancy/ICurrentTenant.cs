namespace Suppy.InventoryUpdate.Api.Abstractions.Tenancy;

public interface ICurrentTenant
{
    bool IsResolved { get; }

    string? TenantId { get; }
}
