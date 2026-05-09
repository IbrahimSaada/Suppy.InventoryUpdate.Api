using Suppy.InventoryUpdate.Api.Domain.ValueObjects;

namespace Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;

public sealed class ItemId : ValueObject
{
    public const int MaxLength = 128;

    private ItemId()
    {
    }

    private ItemId(string value)
    {
        Value = value;
    }

    public string Value { get; private set; } = string.Empty;

    public static ItemId From(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Item id is required.", nameof(value));
        }

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Item id cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new ItemId(normalized);
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
