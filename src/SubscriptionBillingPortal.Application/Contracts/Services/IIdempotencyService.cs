namespace SubscriptionBillingPortal.Application.Contracts.Services;

/// <summary>
/// Service for checking and recording idempotency keys to prevent duplicate command execution.
/// The response JSON is stored so that replays transparently return the original result.
/// </summary>
public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task<string?> GetResponseAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid idempotencyKey, string commandName, string responseJson, CancellationToken cancellationToken = default);
}
