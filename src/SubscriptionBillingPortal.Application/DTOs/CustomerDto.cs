namespace SubscriptionBillingPortal.Application.DTOs;

public sealed record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    DateTimeOffset CreatedAt);
