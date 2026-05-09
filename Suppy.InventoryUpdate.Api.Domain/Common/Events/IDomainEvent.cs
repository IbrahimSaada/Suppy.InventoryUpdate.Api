namespace Suppy.InventoryUpdate.Api.Domain.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
