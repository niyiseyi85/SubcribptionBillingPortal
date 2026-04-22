using MediatR;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command to create a new subscription for an existing customer.
/// PlanType and BillingInterval are accepted as strings from the API layer
/// and resolved to domain enums inside the handler.
/// </summary>
public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    string PlanType,
    string BillingInterval,
    Guid IdempotencyKey) : IRequest<SubscriptionDto>;
