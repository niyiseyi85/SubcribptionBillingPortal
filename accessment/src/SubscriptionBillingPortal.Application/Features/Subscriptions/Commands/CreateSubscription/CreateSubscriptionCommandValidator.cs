using FluentValidation;
using SubscriptionBillingPortal.Domain.Enums;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required and must be a valid GUID.");

        RuleFor(x => x.PlanType)
            .NotEmpty().WithMessage("PlanType is required.")
            .Must(v => Enum.TryParse<PlanType>(v, ignoreCase: true, out _))
            .WithMessage($"PlanType must be one of: {string.Join(", ", Enum.GetNames<PlanType>())}.");

        RuleFor(x => x.BillingInterval)
            .NotEmpty().WithMessage("BillingInterval is required.")
            .Must(v => Enum.TryParse<BillingInterval>(v, ignoreCase: true, out _))
            .WithMessage($"BillingInterval must be one of: {string.Join(", ", Enum.GetNames<BillingInterval>())}.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("An idempotency key is required.");
    }
}
