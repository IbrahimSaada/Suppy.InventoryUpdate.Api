using Suppy.InventoryUpdate.Api.Domain.Events;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Events;

public sealed class ProductUpdateBatchAcceptedDomainEvent : DomainEvent
{
    public ProductUpdateBatchAcceptedDomainEvent(
        string tenantId,
        Guid batchId,
        int totalItems,
        string? idempotencyKey)
    {
        TenantId = tenantId;
        BatchId = batchId;
        TotalItems = totalItems;
        IdempotencyKey = idempotencyKey;
    }

    public string TenantId { get; }
    public Guid BatchId { get; }
    public int TotalItems { get; }
    public string? IdempotencyKey { get; }
}
