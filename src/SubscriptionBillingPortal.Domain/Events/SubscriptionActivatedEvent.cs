using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Domain.Events;

/// <summary>
/// Raised when a subscription is successfully activated.
/// </summary>
public sealed record SubscriptionActivatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid SubscriptionId,
    Guid CustomerId) : IDomainEvent;
