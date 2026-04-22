using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Domain.Events;

/// <summary>
/// Raised when an invoice payment is successfully received.
/// </summary>
public sealed record PaymentReceivedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid InvoiceId,
    Guid SubscriptionId) : IDomainEvent;
