namespace Suppy.InventoryUpdate.Api.Abstractions.Tenancy;

public static class TenantConstants
{
    public const int MaxTenantIdLength = 100;
    public const string HeaderName = "X-Tenant-Id";
    public const string ClaimType = "tenant_id";
}
