using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

internal static class CursorTokenSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string EncodeOffset(int offset, string? protectionKey)
    {
        var normalizedOffset = Math.Max(offset, 0);
        var unsignedPayload = new UnsignedCursorPayload(normalizedOffset);
        var unsignedJson = JsonSerializer.Serialize(unsignedPayload, JsonOptions);

        var signature = ComputeSignature(unsignedJson, protectionKey);
        var payload = new CursorPayload(normalizedOffset, signature);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

        return Base64UrlEncode(bytes);
    }

    public static int DecodeOffset(string? cursor, string? protectionKey)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return 0;
        }

        try
        {
            var bytes = Base64UrlDecode(cursor);
            var payload = JsonSerializer.Deserialize<CursorPayload>(bytes, JsonOptions);
            if (payload is null)
            {
                throw new ArgumentException("Cursor payload is empty.", nameof(cursor));
            }

            if (payload.Offset < 0)
            {
                throw new ArgumentException("Cursor offset cannot be negative.", nameof(cursor));
            }

            if (string.IsNullOrWhiteSpace(protectionKey))
            {
                return payload.Offset;
            }

            var unsignedJson = JsonSerializer.Serialize(new UnsignedCursorPayload(payload.Offset), JsonOptions);
            var expectedSignature = ComputeSignature(unsignedJson, protectionKey);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature),
                    Encoding.UTF8.GetBytes(payload.Signature ?? string.Empty)))
            {
                throw new ArgumentException("Cursor signature validation failed.", nameof(cursor));
            }

            return payload.Offset;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid cursor format.", nameof(cursor), ex);
        }
    }

    private static string ComputeSignature(string payload, string? protectionKey)
    {
        if (string.IsNullOrWhiteSpace(protectionKey))
        {
            return string.Empty;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(protectionKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert
            .ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string data)
    {
        var normalized = data
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = 4 - (normalized.Length % 4);
        if (padding is > 0 and < 4)
        {
            normalized = normalized.PadRight(normalized.Length + padding, '=');
        }

        return Convert.FromBase64String(normalized);
    }

    private sealed record CursorPayload(int Offset, string Signature);

    private sealed record UnsignedCursorPayload(int Offset);
}
