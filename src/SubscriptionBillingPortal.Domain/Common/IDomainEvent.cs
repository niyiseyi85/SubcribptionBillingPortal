namespace SubscriptionBillingPortal.Domain.Common;

/// <summary>
/// Marker interface for all domain events.
/// Domain events are raised inside aggregates and dispatched by the Application layer.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
