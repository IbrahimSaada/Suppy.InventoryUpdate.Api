using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ProcessBatchUpdate;

public sealed record ProcessProductBatchUpdateCommand(Guid BatchId)
    : ICommand<ProcessProductBatchUpdateResult>;
