using MediatR;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;

/// <summary>
/// Command to activate an existing subscription.
/// Triggers the first invoice generation as a domain side-effect.
/// </summary>
public sealed record ActivateSubscriptionCommand(
    Guid SubscriptionId,
    Guid IdempotencyKey) : IRequest<SubscriptionDto>;
