namespace Suppy.InventoryUpdate.Api.Abstractions.Security;

public static class PermissionPolicyName
{
    public const string Prefix = "perm:";

    public static string Build(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission is required.", nameof(permission));
        }

        return $"{Prefix}{permission.Trim()}";
    }

    public static bool TryParse(string? policyName, out string permission)
    {
        permission = string.Empty;

        if (string.IsNullOrWhiteSpace(policyName) ||
            !policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        permission = policyName[Prefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(permission);
    }
}
