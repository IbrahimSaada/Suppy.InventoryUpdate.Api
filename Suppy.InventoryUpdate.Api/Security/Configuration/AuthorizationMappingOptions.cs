using System.Security.Claims;

namespace Suppy.InventoryUpdate.Api.Security.Configuration;

public sealed class AuthorizationMappingOptions
{
    public const string SectionName = "AuthorizationMapping";

    public string KeycloakRealmAccessClaimType { get; set; } = "realm_access";

    public string KeycloakResourceAccessClaimType { get; set; } = "resource_access";

    public bool IncludeKeycloakRealmRoles { get; set; } = true;

    public bool IncludeKeycloakClientRoles { get; set; } = true;

    public List<string> KeycloakClientRoleClients { get; set; } = new();

    public List<string> RoleClaimTypes { get; set; } = new()
    {
        ClaimTypes.Role,
        "role",
        "roles"
    };

    public List<string> PermissionClaimTypes { get; set; } = new()
    {
        "permission",
        "permissions",
        "scope",
        "scp"
    };

    public Dictionary<string, string[]> RolePermissions { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
