using System.Security.Claims;

namespace Suppy.InventoryUpdate.Api.Security.Configuration;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; set; }

    public string Authority { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string NameClaimType { get; set; } = ClaimTypes.NameIdentifier;

    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public int ClockSkewSeconds { get; set; } = 30;

    public List<string> Audiences { get; set; } = new();
}
