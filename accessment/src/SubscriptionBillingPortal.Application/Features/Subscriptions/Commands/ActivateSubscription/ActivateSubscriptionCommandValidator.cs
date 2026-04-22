using FluentValidation;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;

public sealed class ActivateSubscriptionCommandValidator : AbstractValidator<ActivateSubscriptionCommand>
{
    public ActivateSubscriptionCommandValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("SubscriptionId is required and must be a valid GUID.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("An idempotency key is required.");
    }
}
