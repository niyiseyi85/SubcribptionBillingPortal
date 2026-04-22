using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Events;
using SubscriptionBillingPortal.Domain.Exceptions;

namespace SubscriptionBillingPortal.Domain.Entities;

/// <summary>
/// Invoice entity — exists within the Subscription aggregate boundary.
/// All state transitions are enforced through domain methods.
/// </summary>
public sealed class Invoice : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>
    /// Caller-supplied reference used as the idempotency key at the domain level.
    /// Paying with the same reference on an already-paid invoice is a safe no-op.
    /// Paying with a different reference on an already-paid invoice throws.
    /// </summary>
    public string? PaymentReference { get; private set; }

    private Invoice() { }

    /// <summary>
    /// Factory method — the only legitimate way to create an invoice.
    /// </summary>
    internal static Invoice Create(Guid subscriptionId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Invoice amount must be greater than zero.");
        }

        return new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Amount = amount,
            Status = InvoiceStatus.Pending,
            IssuedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Marks the invoice as paid. Raises a PaymentReceivedEvent.
    /// Idempotency rule: same PaymentReference on an already-paid invoice is a no-op.
    /// </summary>
    public void Pay(string paymentReference, Action<IDomainEvent> raiseEvent)
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            throw new DomainException("Payment reference is required.");
        }

        if (Status == InvoiceStatus.Paid)
        {
            if (PaymentReference == paymentReference)
            {
                return; // idempotent: same reference means already processed — safe no-op
            }

            throw new DomainException($"Invoice '{Id}' has already been paid.");
        }

        Status = InvoiceStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        PaymentReference = paymentReference;

        raiseEvent(new PaymentReceivedEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            InvoiceId: Id,
            SubscriptionId: SubscriptionId));
    }
}
