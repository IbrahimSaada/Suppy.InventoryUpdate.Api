using Suppy.InventoryUpdate.Api.Abstractions.Results;

namespace Suppy.InventoryUpdate.Api.Application.Contracts.Errors;

public static class ApplicationErrors
{
    public static Error Validation(string? details = null)
    {
        var message = string.IsNullOrWhiteSpace(details)
            ? "One or more validation errors occurred."
            : $"One or more validation errors occurred: {details}";

        return new Error("application.validation", message);
    }

    public static Error NotFound(string resourceName, string resourceId)
    {
        return new Error("application.not_found", $"{resourceName} '{resourceId}' was not found.");
    }

    public static Error Conflict(string details)
    {
        return new Error("application.conflict", details);
    }

    public static Error Forbidden(string details = "Operation is forbidden.")
    {
        return new Error("application.forbidden", details);
    }

    public static Error Unauthorized(string details = "Authentication is required.")
    {
        return new Error("application.unauthorized", details);
    }

    public static Error Unexpected(string details = "An unexpected application error occurred.")
    {
        return new Error("application.unexpected", details);
    }
}
