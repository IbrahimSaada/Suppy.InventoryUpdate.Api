using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.RetryBatchUpdate;

public sealed record RetryProductBatchUpdateCommand(Guid BatchId)
    : ICommand<RetryProductBatchUpdateResult>;
