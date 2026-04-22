using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Domain.Events;

/// <summary>
/// Raised when a subscription is cancelled.
/// Downstream handlers can react by notifying the customer,
/// stopping scheduled billing jobs, or updating reporting.
/// </summary>
public sealed record SubscriptionCancelledEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid SubscriptionId,
    Guid CustomerId) : IDomainEvent;
