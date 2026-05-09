using Suppy.InventoryUpdate.Api.Domain.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Entities;

public sealed class ProductUpdateBatchItem : TenantScopedEntity
{
    public const int MaxErrorMessageLength = 1_000;

    private ProductUpdateBatchItem()
    {
    }

    private ProductUpdateBatchItem(
        TenantId tenantId,
        Guid batchId,
        ProductUpdateBatchItemDraft draft)
        : base(tenantId)
    {
        BatchId = EnsureNonEmptyBatchId(batchId);
        ItemId = draft.ItemId;
        Price = draft.Price;
        Stock = draft.Stock;
        MetadataJson = draft.MetadataJson;
        Status = ProductUpdateBatchItemStatus.Pending;
    }

    public Guid BatchId { get; private set; }
    public ItemId ItemId { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string? MetadataJson { get; private set; }
    public ProductUpdateBatchItemStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public static ProductUpdateBatchItem Create(
        TenantId tenantId,
        Guid batchId,
        ProductUpdateBatchItemDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new ProductUpdateBatchItem(tenantId, batchId, draft);
    }

    public void MarkProcessing(DateTime utcNow)
    {
        if (Status is ProductUpdateBatchItemStatus.Processed or ProductUpdateBatchItemStatus.Failed)
        {
            return;
        }

        Status = ProductUpdateBatchItemStatus.Processing;
        ProcessingStartedAtUtc ??= utcNow;
    }

    public bool MarkProcessed(DateTime utcNow)
    {
        if (Status == ProductUpdateBatchItemStatus.Processed)
        {
            return false;
        }

        if (Status == ProductUpdateBatchItemStatus.Failed)
        {
            throw new InvalidOperationException("Failed batch items cannot be marked processed without retrying first.");
        }

        Status = ProductUpdateBatchItemStatus.Processed;
        ErrorMessage = null;
        ProcessedAtUtc = utcNow;

        return true;
    }

    public bool MarkFailed(string errorMessage, DateTime utcNow)
    {
        if (Status == ProductUpdateBatchItemStatus.Failed)
        {
            return false;
        }

        if (Status == ProductUpdateBatchItemStatus.Processed)
        {
            throw new InvalidOperationException("Processed batch items cannot be marked failed.");
        }

        Status = ProductUpdateBatchItemStatus.Failed;
        ErrorMessage = NormalizeError(errorMessage);
        FailedAtUtc = utcNow;

        return true;
    }

    private static Guid EnsureNonEmptyBatchId(Guid batchId)
    {
        if (batchId == Guid.Empty)
        {
            throw new ArgumentException("Batch id cannot be empty.", nameof(batchId));
        }

        return batchId;
    }

    private static string NormalizeError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Unknown processing error.";
        }

        var normalized = errorMessage.Trim();
        return normalized.Length <= MaxErrorMessageLength
            ? normalized
            : normalized[..MaxErrorMessageLength];
    }
}
