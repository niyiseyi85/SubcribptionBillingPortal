using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Domain.Entities;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Events;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Domain.Aggregates;

/// <summary>
/// Subscription aggregate root.
/// Encapsulates all business rules around the subscription lifecycle and invoice generation.
/// No public setters — all state transitions go through domain methods.
/// </summary>
public sealed class Subscription : AggregateRoot
{
    private readonly List<Invoice> _invoices = [];

    public Guid CustomerId { get; private set; }
    public SubscriptionPlan Plan { get; private set; } = null!;
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? LastBillingDate { get; private set; }
    public DateTimeOffset? NextBillingDate { get; private set; }

    public IReadOnlyCollection<Invoice> Invoices => _invoices.AsReadOnly();

    private Subscription() { }

    /// <summary>
    /// Factory method — creates a new subscription in Inactive state.
    /// </summary>
    public static Subscription Create(Guid customerId, SubscriptionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Plan = plan,
            Status = SubscriptionStatus.Inactive,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Activates the subscription and generates the first invoice.
    /// Enforces: cannot activate an already-active or cancelled subscription.
    /// </summary>
    public void Activate()
    {
        if (Status == SubscriptionStatus.Active)
        {
            throw new DomainException($"Subscription '{Id}' is already active.");
        }

        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException($"Subscription '{Id}' has been cancelled and cannot be reactivated.");
        }

        Status = SubscriptionStatus.Active;
        ActivatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new SubscriptionActivatedEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            SubscriptionId: Id,
            CustomerId: CustomerId));

        GenerateInvoiceInternal();
    }

    /// <summary>
    /// Cancels the subscription. Stops future invoice generation.
    /// Enforces: cannot cancel an already-cancelled subscription.
    /// </summary>
    public void Cancel()
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException($"Subscription '{Id}' is already cancelled.");
        }

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new SubscriptionCancelledEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            SubscriptionId: Id,
            CustomerId: CustomerId));
    }

    /// <summary>
    /// Public method to generate a new invoice for an active subscription.
    /// Enforces: subscription must be active AND billing date must be due.
    /// </summary>
    public void GenerateInvoice()
    {
        if (Status != SubscriptionStatus.Active)
        {
            throw new DomainException($"Cannot generate invoice for subscription '{Id}' — subscription is not active.");
        }

        GenerateInvoiceInternal();
    }

    /// <summary>
    /// Pays a specific invoice belonging to this subscription.
    /// Delegates payment logic to the Invoice entity.
    /// PaymentReference enables idempotency: same reference on an already-paid invoice is a no-op.
    /// </summary>
    public void PayInvoice(Guid invoiceId, string paymentReference)
    {
        var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId)
            ?? throw new DomainException($"Invoice '{invoiceId}' not found in subscription '{Id}'.");

        invoice.Pay(paymentReference, RaiseDomainEvent);
    }

    private void GenerateInvoiceInternal()
    {
        var invoice = Invoice.Create(Id, Plan.Price);
        _invoices.Add(invoice);

        LastBillingDate = DateTimeOffset.UtcNow;
        NextBillingDate = DateTimeOffset.UtcNow.AddDays(Plan.BillingIntervalDays);

        RaiseDomainEvent(new InvoiceGeneratedEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            InvoiceId: invoice.Id,
            SubscriptionId: Id));
    }
}
