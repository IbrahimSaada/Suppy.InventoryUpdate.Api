using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

public sealed record SubmitProductBatchUpdateCommand(
    string TenantId,
    IReadOnlyCollection<SubmitProductBatchUpdateItem> Items,
    string? IdempotencyKey) : ICommand<SubmitProductBatchUpdateResult>;
