namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

public sealed record SubmitProductBatchUpdateResult(
    Guid BatchId,
    string TenantId,
    string Status,
    int TotalItems,
    bool WasDuplicate);
