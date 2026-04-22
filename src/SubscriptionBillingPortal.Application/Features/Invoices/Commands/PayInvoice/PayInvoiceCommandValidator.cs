using FluentValidation;

namespace SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;

public sealed class PayInvoiceCommandValidator : AbstractValidator<PayInvoiceCommand>
{
    public PayInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("InvoiceId is required and must be a valid GUID.");

        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("SubscriptionId is required and must be a valid GUID.");

        RuleFor(x => x.PaymentReference)
            .NotEmpty().WithMessage("PaymentReference is required.")
            .MaximumLength(200).WithMessage("PaymentReference must not exceed 200 characters.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("An idempotency key is required.");
    }
}
