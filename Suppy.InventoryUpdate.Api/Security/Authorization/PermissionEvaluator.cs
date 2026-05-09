using Suppy.InventoryUpdate.Api.Abstractions.Security;

namespace Suppy.InventoryUpdate.Api.Security.Authorization;

internal sealed class PermissionEvaluator : IPermissionEvaluator
{
    public Task<bool> HasPermissionAsync(
        ICurrentUser currentUser,
        string permission,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(permission))
        {
            return Task.FromResult(false);
        }

        var normalizedPermission = permission.Trim();

        foreach (var grantedPermission in currentUser.Permissions)
        {
            if (IsMatch(grantedPermission, normalizedPermission))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private static bool IsMatch(string grantedPermission, string requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(grantedPermission))
        {
            return false;
        }

        var granted = grantedPermission.Trim();

        if (string.Equals(granted, "*", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(granted, requiredPermission, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!granted.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var prefix = granted[..^2];
        return requiredPermission.StartsWith($"{prefix}.", StringComparison.OrdinalIgnoreCase);
    }
}
