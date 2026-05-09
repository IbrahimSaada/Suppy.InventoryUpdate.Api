using Microsoft.AspNetCore.Authorization;

namespace Suppy.InventoryUpdate.Api.Security.Authorization;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission is required.", nameof(permission));
        }

        Permission = permission.Trim();
    }

    public string Permission { get; }
}
