namespace Suppy.InventoryUpdate.Api.Abstractions.Security;

public interface IPermissionEvaluator
{
    Task<bool> HasPermissionAsync(
        ICurrentUser currentUser,
        string permission,
        CancellationToken ct = default);
}
