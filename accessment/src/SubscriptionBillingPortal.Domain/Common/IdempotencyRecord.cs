namespace SubscriptionBillingPortal.Domain.Common;

/// <summary>
/// Tracks idempotency keys for commands to prevent duplicate processing.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid IdempotencyKey { get; private set; }
    public string CommandName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(Guid idempotencyKey, string commandName)
    {
        return new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            CommandName = commandName,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
