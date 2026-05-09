using Suppy.InventoryUpdate.Api.Domain.Events;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Events;

public sealed class ProductUpdateBatchCompletedDomainEvent : DomainEvent
{
    public ProductUpdateBatchCompletedDomainEvent(
        string tenantId,
        Guid batchId,
        ProductUpdateBatchStatus status,
        int processedItems,
        int failedItems)
    {
        TenantId = tenantId;
        BatchId = batchId;
        Status = status;
        ProcessedItems = processedItems;
        FailedItems = failedItems;
    }

    public string TenantId { get; }
    public Guid BatchId { get; }
    public ProductUpdateBatchStatus Status { get; }
    public int ProcessedItems { get; }
    public int FailedItems { get; }
}
