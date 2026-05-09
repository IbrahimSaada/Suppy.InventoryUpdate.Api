using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ListProducts;

public sealed record ListProductsQuery(
    string TenantId,
    int Page,
    int PageSize) : IQuery<ListProductsResult>;
