namespace Suppy.InventoryUpdate.Api.Presentation;

public sealed record ApiEnvelope<TData>(
    string Message,
    DateTime DateTimeUtc,
    TData Data)
{
    public static ApiEnvelope<TData> Success(TData data, string message = "Success")
    {
        return new ApiEnvelope<TData>(message, DateTime.UtcNow, data);
    }

    public static ApiEnvelope<TData> Failure(string message, TData data)
    {
        return new ApiEnvelope<TData>(message, DateTime.UtcNow, data);
    }
}

public sealed record ApiErrorPayload(
    string ErrorCode,
    int StatusCode,
    string? TraceId = null,
    string? Detail = null);
