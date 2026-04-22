using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Domain.Events;

/// <summary>
/// Raised when an invoice is generated for a subscription.
/// </summary>
public sealed record InvoiceGeneratedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid InvoiceId,
    Guid SubscriptionId) : IDomainEvent;
