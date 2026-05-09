using System.Security.Claims;
using System.Text.Json;
using Suppy.InventoryUpdate.Api.Abstractions.Security;
using Suppy.InventoryUpdate.Api.Security.Configuration;
using Microsoft.Extensions.Options;

namespace Suppy.InventoryUpdate.Api.Security.CurrentUser;

internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthorizationMappingOptions _mappingOptions;
    private readonly ILogger<HttpContextCurrentUser> _logger;
    private CurrentUserSnapshot? _snapshot;

    public HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthorizationMappingOptions> mappingOptions,
        ILogger<HttpContextCurrentUser> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _mappingOptions = mappingOptions.Value;
        _logger = logger;
    }

    public bool IsAuthenticated => Snapshot.IsAuthenticated;

    public string? SubjectId => Snapshot.SubjectId;

    public string? UserName => Snapshot.UserName;

    public IReadOnlyCollection<string> Roles => Snapshot.Roles;

    public IReadOnlyCollection<string> Permissions => Snapshot.Permissions;

    private CurrentUserSnapshot Snapshot => _snapshot ??= BuildSnapshot();

    private CurrentUserSnapshot BuildSnapshot()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return CurrentUserSnapshot.Anonymous;
        }

        var roles = ExtractRoles(principal);
        var permissions = ExtractPermissions(principal);
        ExpandPermissionsFromRoles(roles, permissions);

        var subjectId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
            principal.FindFirstValue("sub");

        var userName =
            principal.Identity?.Name ??
            principal.FindFirstValue("preferred_username") ??
            principal.FindFirstValue(ClaimTypes.Name);

        return new CurrentUserSnapshot(
            isAuthenticated: true,
            subjectId: subjectId,
            userName: userName,
            roles: roles.ToArray(),
            permissions: permissions.ToArray());
    }

    private HashSet<string> ExtractRoles(ClaimsPrincipal principal)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var roleClaimTypes = _mappingOptions.RoleClaimTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var claimType in roleClaimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                AddSplitValues(roles, claim.Value);
            }
        }

        AppendKeycloakRealmRoles(principal, roles);
        AppendKeycloakClientRoles(principal, roles);

        return roles;
    }

    private void AppendKeycloakRealmRoles(ClaimsPrincipal principal, HashSet<string> roles)
    {
        if (!_mappingOptions.IncludeKeycloakRealmRoles)
        {
            return;
        }

        foreach (var claim in principal.Claims.Where(c =>
                     string.Equals(
                         c.Type,
                         _mappingOptions.KeycloakRealmAccessClaimType,
                         StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(claim.Value);
                if (!document.RootElement.TryGetProperty("roles", out var rolesNode) ||
                    rolesNode.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var roleNode in rolesNode.EnumerateArray())
                {
                    if (roleNode.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var role = roleNode.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role.Trim());
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to parse realm_access roles claim.");
            }
        }
    }

    private void AppendKeycloakClientRoles(ClaimsPrincipal principal, HashSet<string> roles)
    {
        if (!_mappingOptions.IncludeKeycloakClientRoles)
        {
            return;
        }

        var allowedClients = _mappingOptions.KeycloakClientRoleClients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowAllClients = allowedClients.Count == 0;

        foreach (var claim in principal.Claims.Where(c =>
                     string.Equals(
                         c.Type,
                         _mappingOptions.KeycloakResourceAccessClaimType,
                         StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(claim.Value);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var clientNode in document.RootElement.EnumerateObject())
                {
                    if (!allowAllClients && !allowedClients.Contains(clientNode.Name))
                    {
                        continue;
                    }

                    if (!clientNode.Value.TryGetProperty("roles", out var rolesNode) ||
                        rolesNode.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var roleNode in rolesNode.EnumerateArray())
                    {
                        if (roleNode.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var role = roleNode.GetString();
                        if (!string.IsNullOrWhiteSpace(role))
                        {
                            roles.Add(role.Trim());
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to parse resource_access roles claim.");
            }
        }
    }

    private HashSet<string> ExtractPermissions(ClaimsPrincipal principal)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var permissionClaimTypes = _mappingOptions.PermissionClaimTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var claimType in permissionClaimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                AddSplitValues(permissions, claim.Value);
            }
        }

        return permissions;
    }

    private void ExpandPermissionsFromRoles(
        IEnumerable<string> roles,
        HashSet<string> permissions)
    {
        foreach (var role in roles)
        {
            if (!_mappingOptions.RolePermissions.TryGetValue(role, out var mappedPermissions))
            {
                continue;
            }

            foreach (var mappedPermission in mappedPermissions ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(mappedPermission))
                {
                    permissions.Add(mappedPermission.Trim());
                }
            }
        }
    }

    private static void AddSplitValues(HashSet<string> target, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var value = rawValue.Trim();

        if (value.StartsWith("[", StringComparison.Ordinal) &&
            value.EndsWith("]", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var parsed = item.GetString();
                        if (!string.IsNullOrWhiteSpace(parsed))
                        {
                            target.Add(parsed.Trim());
                        }
                    }

                    return;
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // Fall through and treat as normal delimited string.
            }
        }

        var separators = value.Contains(' ') && !value.Contains(',')
            ? new[] { ' ' }
            : new[] { ',', ';', ' ' };

        foreach (var item in value.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                target.Add(item.Trim());
            }
        }
    }
}
