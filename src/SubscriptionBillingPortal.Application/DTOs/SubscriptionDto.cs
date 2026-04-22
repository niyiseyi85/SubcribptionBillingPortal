namespace SubscriptionBillingPortal.Application.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    Guid CustomerId,
    string PlanType,
    string BillingInterval,
    decimal PlanPrice,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? CancelledAt);
