namespace Suppy.InventoryUpdate.Api.Infrastructure.RateLimiting;

internal sealed class TenantRateLimitingOptions
{
    public const string SectionName = "RateLimiting:Tenant";

    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 120;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; }

    public bool IncludeQueryStringTenantId { get; set; } = true;
}
