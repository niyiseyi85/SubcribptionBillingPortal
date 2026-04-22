using MediatR;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;

/// <summary>
/// Command to pay a specific invoice.
/// PaymentReference is used as a domain-level idempotency key:
/// retrying with the same reference on an already-paid invoice is a safe no-op.
/// </summary>
public sealed record PayInvoiceCommand(
    Guid InvoiceId,
    Guid SubscriptionId,
    string PaymentReference,
    Guid IdempotencyKey) : IRequest<InvoiceDto>;
