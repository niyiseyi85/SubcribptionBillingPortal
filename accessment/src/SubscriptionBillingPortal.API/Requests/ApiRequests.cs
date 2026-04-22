namespace SubscriptionBillingPortal.API.Requests;

public sealed record CreateCustomerRequest(string FirstName, string LastName, string Email);

public sealed record CreateSubscriptionRequest(Guid CustomerId, string PlanType, string BillingInterval);

/// <summary>
/// PaymentReference is required and acts as the domain-level idempotency key:
/// retrying the same payment with the same reference is a safe no-op.
/// </summary>
public sealed record PayInvoiceRequest(Guid SubscriptionId, string PaymentReference);
