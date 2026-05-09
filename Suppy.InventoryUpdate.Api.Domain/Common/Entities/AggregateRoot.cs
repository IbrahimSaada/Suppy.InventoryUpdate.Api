using Suppy.InventoryUpdate.Api.Domain.Events;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class AggregateRoot<TKey> : Entity<TKey> where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
