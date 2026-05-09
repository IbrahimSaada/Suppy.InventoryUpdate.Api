using Suppy.InventoryUpdate.Api.Domain.ValueObjects;

namespace Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;

public sealed class BatchIdempotencyKey : ValueObject
{
    public const int MaxLength = 200;

    private BatchIdempotencyKey()
    {
    }

    private BatchIdempotencyKey(string value)
    {
        Value = value;
    }

    public string Value { get; private set; } = string.Empty;

    public static BatchIdempotencyKey From(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Idempotency key is required when provided.", nameof(value));
        }

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Idempotency key cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new BatchIdempotencyKey(normalized);
    }

    public static BatchIdempotencyKey? FromOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : From(value);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }
}
