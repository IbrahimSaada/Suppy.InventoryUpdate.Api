using System.Text.Json;
using Suppy.InventoryUpdate.Api.Abstractions.Caching;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Caching;

internal sealed class SystemTextJsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> payload)
    {
        return JsonSerializer.Deserialize<T>(payload.Span, JsonOptions);
    }
}
