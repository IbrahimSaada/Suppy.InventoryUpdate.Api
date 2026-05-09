using Suppy.InventoryUpdate.Api.Domain.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Events;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Entities;

public sealed class Product : TenantScopedAggregateRoot
{
    private Product()
    {
    }

    private Product(
        TenantId tenantId,
        ItemId itemId,
        decimal price,
        int stock,
        string? metadataJson,
        Guid sourceBatchId)
        : base(tenantId)
    {
        ItemId = itemId ?? throw new ArgumentNullException(nameof(itemId));
        Price = EnsureNonNegativePrice(price);
        Stock = EnsureNonNegativeStock(stock);
        MetadataJson = NormalizeMetadata(metadataJson);
        LastBatchId = EnsureNonEmptyBatchId(sourceBatchId);
    }

    public ItemId ItemId { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string? MetadataJson { get; private set; }
    public Guid LastBatchId { get; private set; }
    public DateTime LastUpdatedFromBatchAtUtc { get; private set; }

    public static Product Create(
        TenantId tenantId,
        ItemId itemId,
        decimal price,
        int stock,
        string? metadataJson,
        Guid sourceBatchId,
        DateTime utcNow)
    {
        var product = new Product(tenantId, itemId, price, stock, metadataJson, sourceBatchId)
        {
            LastUpdatedFromBatchAtUtc = utcNow
        };

        product.AddDomainEvent(new ProductUpsertedFromBatchDomainEvent(
            tenantId.Value,
            product.Id,
            itemId.Value,
            sourceBatchId));

        return product;
    }

    public void ApplyUpdate(
        decimal price,
        int stock,
        string? metadataJson,
        Guid sourceBatchId,
        DateTime utcNow)
    {
        Price = EnsureNonNegativePrice(price);
        Stock = EnsureNonNegativeStock(stock);
        MetadataJson = NormalizeMetadata(metadataJson);
        LastBatchId = EnsureNonEmptyBatchId(sourceBatchId);
        LastUpdatedFromBatchAtUtc = utcNow;

        AddDomainEvent(new ProductUpsertedFromBatchDomainEvent(
            TenantId.Value,
            Id,
            ItemId.Value,
            sourceBatchId));
    }

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

    private static Guid EnsureNonEmptyBatchId(Guid batchId)
    {
        if (batchId == Guid.Empty)
        {
            throw new ArgumentException("Batch id cannot be empty.", nameof(batchId));
        }

        return batchId;
    }

    private static string? NormalizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        var normalized = metadataJson.Trim();
        if (normalized.Length > ProductUpdateBatchItemDraft.MaxMetadataJsonLength)
        {
            throw new ArgumentException(
                $"Metadata JSON cannot exceed {ProductUpdateBatchItemDraft.MaxMetadataJsonLength} characters.",
                nameof(metadataJson));
        }

        return normalized;
    }
}
