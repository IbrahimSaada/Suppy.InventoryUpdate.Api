namespace Suppy.InventoryUpdate.Api.Domain.Products.Enums;

public enum ProductUpdateBatchStatus
{
    Accepted = 1,
    Processing = 2,
    Completed = 3,
    PartiallyFailed = 4,
    Failed = 5
}
