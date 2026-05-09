namespace Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;

public sealed record ProductUpdateBatchItemDraft
{
    public const int MaxMetadataJsonLength = 16_000;

    public ProductUpdateBatchItemDraft(
        ItemId itemId,
        decimal price,
        int stock,
        string? metadataJson)
    {
        ItemId = itemId ?? throw new ArgumentNullException(nameof(itemId));
        Price = EnsureNonNegativePrice(price);
        Stock = EnsureNonNegativeStock(stock);
        MetadataJson = NormalizeMetadata(metadataJson);
    }

    public ItemId ItemId { get; }
    public decimal Price { get; }
    public int Stock { get; }
    public string? MetadataJson { get; }

    private static decimal EnsureNonNegativePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        return price;
    }

    private static int EnsureNonNegativeStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock cannot be negative.");
        }

        return stock;
    }

    private static string? NormalizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        var normalized = metadataJson.Trim();
        if (normalized.Length > MaxMetadataJsonLength)
        {
            throw new ArgumentException(
                $"Metadata JSON cannot exceed {MaxMetadataJsonLength} characters.",
                nameof(metadataJson));
        }

        return normalized;
    }
}
