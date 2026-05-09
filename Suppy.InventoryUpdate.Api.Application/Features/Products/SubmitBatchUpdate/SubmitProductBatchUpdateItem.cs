namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

public sealed record SubmitProductBatchUpdateItem(
    string ItemId,
    decimal Price,
    int Stock,
    string? MetadataJson);
