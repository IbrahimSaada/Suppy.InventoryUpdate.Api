using Suppy.InventoryUpdate.Api.Abstractions.Security;

namespace Suppy.InventoryUpdate.Api.Security.CurrentUser;

internal sealed class CurrentUserSnapshot : ICurrentUser
{
    public static CurrentUserSnapshot Anonymous { get; } = new(
        isAuthenticated: false,
        subjectId: null,
        userName: null,
        roles: Array.Empty<string>(),
        permissions: Array.Empty<string>());

    public CurrentUserSnapshot(
        bool isAuthenticated,
        string? subjectId,
        string? userName,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        IsAuthenticated = isAuthenticated;
        SubjectId = subjectId;
        UserName = userName;
        Roles = roles;
        Permissions = permissions;
    }

    public bool IsAuthenticated { get; }

    public string? SubjectId { get; }

    public string? UserName { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Permissions { get; }
}
