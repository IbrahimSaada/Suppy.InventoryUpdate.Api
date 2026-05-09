using Suppy.InventoryUpdate.Api.Domain.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;
using Suppy.InventoryUpdate.Api.Domain.Products.Events;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Entities;

public sealed class ProductUpdateBatch : TenantScopedAggregateRoot
{
    private readonly List<ProductUpdateBatchItem> _items = new();

    private ProductUpdateBatch()
    {
    }

    private ProductUpdateBatch(
        TenantId tenantId,
        BatchIdempotencyKey? idempotencyKey,
        IReadOnlyCollection<ProductUpdateBatchItemDraft> itemDrafts)
        : base(tenantId)
    {
        IdempotencyKey = idempotencyKey;
        Status = ProductUpdateBatchStatus.Accepted;
        TotalItems = EnsureValidItems(itemDrafts);

        foreach (var draft in itemDrafts)
        {
            _items.Add(ProductUpdateBatchItem.Create(tenantId, Id, draft));
        }
    }

    public BatchIdempotencyKey? IdempotencyKey { get; private set; }
    public ProductUpdateBatchStatus Status { get; private set; }
    public int TotalItems { get; private set; }
    public int ProcessedItems { get; private set; }
    public int FailedItems { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyCollection<ProductUpdateBatchItem> Items => _items.AsReadOnly();

    public static ProductUpdateBatch Accept(
        TenantId tenantId,
        BatchIdempotencyKey? idempotencyKey,
        IReadOnlyCollection<ProductUpdateBatchItemDraft> itemDrafts)
    {
        var batch = new ProductUpdateBatch(tenantId, idempotencyKey, itemDrafts);

        batch.AddDomainEvent(new ProductUpdateBatchAcceptedDomainEvent(
            tenantId.Value,
            batch.Id,
            batch.TotalItems,
            idempotencyKey?.Value));

        return batch;
    }

    public void StartProcessing(DateTime utcNow)
    {
        if (Status != ProductUpdateBatchStatus.Accepted)
        {
            return;
        }

        Status = ProductUpdateBatchStatus.Processing;
        ProcessingStartedAtUtc = utcNow;
    }

    public ProductUpdateBatchItem MarkItemProcessing(Guid itemId, DateTime utcNow)
    {
        var item = FindItem(itemId);
        item.MarkProcessing(utcNow);
        return item;
    }

    public void MarkItemProcessed(Guid itemId, DateTime utcNow)
    {
        EnsureProcessingHasStarted(utcNow);

        var item = FindItem(itemId);
        if (item.MarkProcessed(utcNow))
        {
            ProcessedItems++;
        }

        CompleteIfFinished(utcNow);
    }

    public void MarkItemFailed(Guid itemId, string errorMessage, DateTime utcNow)
    {
        EnsureProcessingHasStarted(utcNow);

        var item = FindItem(itemId);
        if (item.MarkFailed(errorMessage, utcNow))
        {
            FailedItems++;
        }

        CompleteIfFinished(utcNow);
    }

    public void MarkFailed(string failureReason, DateTime utcNow)
    {
        if (Status is ProductUpdateBatchStatus.Completed or ProductUpdateBatchStatus.PartiallyFailed)
        {
            throw new InvalidOperationException("Completed batches cannot be marked failed.");
        }

        Status = ProductUpdateBatchStatus.Failed;
        FailureReason = NormalizeFailureReason(failureReason);
        CompletedAtUtc = utcNow;

        AddDomainEvent(new ProductUpdateBatchCompletedDomainEvent(
            TenantId.Value,
            Id,
            Status,
            ProcessedItems,
            FailedItems));
    }

    public int RetryFailedItems(DateTime utcNow)
    {
        if (Status is not (ProductUpdateBatchStatus.Failed or ProductUpdateBatchStatus.PartiallyFailed))
        {
            throw new InvalidOperationException(
                $"Only failed or partially failed batches can be retried. Current status is '{Status}'.");
        }

        var retryCount = 0;
        foreach (var item in _items)
        {
            if (item.ResetFailedForRetry(utcNow))
            {
                retryCount++;
            }
        }

        if (retryCount == 0)
        {
            throw new InvalidOperationException("Batch has no failed items to retry.");
        }

        FailedItems = Math.Max(0, FailedItems - retryCount);
        Status = ProductUpdateBatchStatus.Accepted;
        FailureReason = null;
        CompletedAtUtc = null;
        UpdatedAtUtc = utcNow;

        return retryCount;
    }

    private static int EnsureValidItems(IReadOnlyCollection<ProductUpdateBatchItemDraft> itemDrafts)
    {
        ArgumentNullException.ThrowIfNull(itemDrafts);

        if (itemDrafts.Count == 0)
        {
            throw new ArgumentException("Batch must contain at least one item.", nameof(itemDrafts));
        }

        var duplicateItemId = itemDrafts
            .GroupBy(item => item.ItemId.Value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateItemId is not null)
        {
            throw new ArgumentException($"Batch contains duplicate item id '{duplicateItemId}'.", nameof(itemDrafts));
        }

        return itemDrafts.Count;
    }

    private ProductUpdateBatchItem FindItem(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Batch item id cannot be empty.", nameof(itemId));
        }

        return _items.FirstOrDefault(item => item.Id == itemId)
            ?? throw new InvalidOperationException($"Batch item '{itemId}' was not found.");
    }

    private void EnsureProcessingHasStarted(DateTime utcNow)
    {
        if (Status == ProductUpdateBatchStatus.Accepted)
        {
            StartProcessing(utcNow);
        }

        if (Status != ProductUpdateBatchStatus.Processing)
        {
            throw new InvalidOperationException($"Batch cannot process items while status is '{Status}'.");
        }
    }

    private void CompleteIfFinished(DateTime utcNow)
    {
        if (ProcessedItems + FailedItems < TotalItems)
        {
            return;
        }

        Status = FailedItems switch
        {
            0 => ProductUpdateBatchStatus.Completed,
            _ when ProcessedItems == 0 => ProductUpdateBatchStatus.Failed,
            _ => ProductUpdateBatchStatus.PartiallyFailed
        };

        CompletedAtUtc = utcNow;

        AddDomainEvent(new ProductUpdateBatchCompletedDomainEvent(
            TenantId.Value,
            Id,
            Status,
            ProcessedItems,
            FailedItems));
    }

    private static string NormalizeFailureReason(string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            return "Unknown batch processing error.";
        }

        var normalized = failureReason.Trim();
        return normalized.Length <= ProductUpdateBatchItem.MaxErrorMessageLength
            ? normalized
            : normalized[..ProductUpdateBatchItem.MaxErrorMessageLength];
    }
}
