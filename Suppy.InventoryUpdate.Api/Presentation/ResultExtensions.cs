using System.Diagnostics;
using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Microsoft.AspNetCore.Mvc;

namespace Suppy.InventoryUpdate.Api.Presentation;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult<TValue>(
        this ControllerBase controller,
        Result<TValue> result,
        string successMessage = "Success")
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return controller.Ok(ApiEnvelope<TValue>.Success(result.Value, successMessage));
        }

        var statusCode = MapStatusCode(result.Error.Code);
        var traceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier;
        var errorCode = string.IsNullOrWhiteSpace(result.Error.Code) ? "unhandled_error" : result.Error.Code;
        var message = string.IsNullOrWhiteSpace(result.Error.Message)
            ? BuildFallbackErrorMessage(statusCode)
            : result.Error.Message;

        var payload = ApiEnvelope<ApiErrorPayload>.Failure(
            message,
            new ApiErrorPayload(errorCode, statusCode, traceId));

        return controller.StatusCode(statusCode, payload);
    }

    private static int MapStatusCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return StatusCodes.Status500InternalServerError;
        }

        if (errorCode.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status409Conflict;
        }

        if (errorCode.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (errorCode.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status401Unauthorized;
        }

        return StatusCodes.Status500InternalServerError;
    }

    private static string BuildFallbackErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request was invalid.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request could not be completed due to a conflict.",
            _ => "An unexpected error occurred."
        };
    }
}
