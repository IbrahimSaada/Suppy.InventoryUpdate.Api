using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.GetBatchStatus;

public sealed record GetProductBatchStatusQuery(Guid BatchId)
    : IQuery<ProductBatchStatusResult>;
