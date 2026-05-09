using Suppy.InventoryUpdate.Api.Domain.Events;

namespace Suppy.InventoryUpdate.Api.Domain.Products.Events;

public sealed class ProductUpsertedFromBatchDomainEvent : DomainEvent
{
    public ProductUpsertedFromBatchDomainEvent(
        string tenantId,
        Guid productId,
        string itemId,
        Guid batchId)
    {
        TenantId = tenantId;
        ProductId = productId;
        ItemId = itemId;
        BatchId = batchId;
    }

    public string TenantId { get; }
    public Guid ProductId { get; }
    public string ItemId { get; }
    public Guid BatchId { get; }
}
