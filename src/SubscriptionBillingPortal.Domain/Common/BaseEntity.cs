namespace SubscriptionBillingPortal.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides identity, equality semantics and domain event collection management.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
