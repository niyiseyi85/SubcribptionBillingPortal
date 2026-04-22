using FluentValidation;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;

public sealed class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("SubscriptionId is required and must be a valid GUID.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("An idempotency key is required.");
    }
}
