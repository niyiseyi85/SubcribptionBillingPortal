namespace SubscriptionBillingPortal.Application.Contracts.Services;

/// <summary>
/// Service for checking and recording idempotency keys to prevent duplicate command execution.
/// </summary>
public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid idempotencyKey, string commandName, CancellationToken cancellationToken = default);
}
