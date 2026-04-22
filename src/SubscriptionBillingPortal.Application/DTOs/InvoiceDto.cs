namespace SubscriptionBillingPortal.Application.DTOs;

public sealed record InvoiceDto(
    Guid Id,
    Guid SubscriptionId,
    decimal Amount,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? PaidAt,
    string? PaymentReference);
