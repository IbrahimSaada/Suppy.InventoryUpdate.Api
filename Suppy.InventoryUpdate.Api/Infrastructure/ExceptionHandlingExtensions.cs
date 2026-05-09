using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Suppy.InventoryUpdate.Api.Presentation;
using Microsoft.AspNetCore.Diagnostics;

namespace Suppy.InventoryUpdate.Api.Infrastructure;

internal static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(
        this IApplicationBuilder app,
        bool includeExceptionDetails)
    {
        ArgumentNullException.ThrowIfNull(app);

        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                var (statusCode, errorCode, message) = MapException(exception);

                if (exception is not null)
                {
                    if (statusCode >= StatusCodes.Status500InternalServerError)
                    {
                        logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
                    }
                    else
                    {
                        logger.LogWarning(exception, "Handled mapped exception. TraceId: {TraceId}", traceId);
                    }
                }
                else
                {
                    logger.LogError("Unhandled exception with no feature error. TraceId: {TraceId}", traceId);
                }

                var detail = BuildDetail(exception, statusCode, includeExceptionDetails);

                var payload = ApiEnvelope<ApiErrorPayload>.Failure(
                    message,
                    new ApiErrorPayload(errorCode, statusCode, traceId, detail));

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json; charset=utf-8";

                await context.Response.WriteAsJsonAsync(payload);
            });
        });

        return app;
    }

    private static (int StatusCode, string ErrorCode, string Message) MapException(Exception? exception)
    {
        return exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "validation_error", "Validation error."),
            ArgumentException argumentException => (
                StatusCodes.Status400BadRequest,
                "bad_request",
                ExtractArgumentMessage(argumentException)),
            FormatException => (StatusCodes.Status400BadRequest, "bad_request", "Bad request."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found", "Resource not found."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden", "Forbidden."),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "timeout", "Operation timed out."),
            _ => (StatusCodes.Status500InternalServerError, "unhandled_error", "Unhandled server error.")
        };
    }

    private static string? BuildDetail(Exception? exception, int statusCode, bool includeExceptionDetails)
    {
        if (statusCode < StatusCodes.Status500InternalServerError)
        {
            return null;
        }

        if (includeExceptionDetails)
        {
            return exception?.ToString() ?? "An unexpected error occurred.";
        }

        return BuildSafeDetail(statusCode);
    }

    private static string ExtractArgumentMessage(ArgumentException exception)
    {
        var message = exception.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Bad request.";
        }

        const string parameterMarker = " (Parameter '";
        var markerIndex = message.IndexOf(parameterMarker, StringComparison.Ordinal);
        if (markerIndex > 0)
        {
            message = message[..markerIndex];
        }

        return message.EndsWith('.') ? message : $"{message}.";
    }

    private static string BuildSafeDetail(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request was invalid.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status504GatewayTimeout => "The operation timed out.",
            _ => "An unexpected error occurred."
        };
    }
}
