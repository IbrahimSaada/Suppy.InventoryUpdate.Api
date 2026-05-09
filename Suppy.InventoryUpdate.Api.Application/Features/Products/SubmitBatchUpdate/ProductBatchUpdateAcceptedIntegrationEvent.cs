using Suppy.InventoryUpdate.Api.Abstractions.Messaging;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

public sealed record ProductBatchUpdateAcceptedIntegrationEvent(
    Guid BatchId,
    int TotalItems) : IntegrationEventBase;
