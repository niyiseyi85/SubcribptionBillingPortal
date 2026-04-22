using MediatR;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel an active or inactive subscription.
/// </summary>
public sealed record CancelSubscriptionCommand(
    Guid SubscriptionId,
    Guid IdempotencyKey) : IRequest<SubscriptionDto>;
