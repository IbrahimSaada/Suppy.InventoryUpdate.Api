namespace Suppy.InventoryUpdate.Api.Infrastructure.Observability;

internal sealed class CorrelationIdMiddleware
{
    private static readonly char[] AllowedCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._".ToCharArray();

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationIdConstants.HttpContextItemKey] = correlationId;
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdConstants.HeaderName, out var values))
        {
            var candidate = values.FirstOrDefault();
            if (IsValid(candidate))
            {
                return candidate!;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return false;
        }

        if (correlationId.Length > 128)
        {
            return false;
        }

        foreach (var ch in correlationId)
        {
            if (!AllowedCharacters.Contains(ch))
            {
                return false;
            }
        }

        return true;
    }
}
