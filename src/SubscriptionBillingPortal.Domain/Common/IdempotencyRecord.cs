namespace SubscriptionBillingPortal.Domain.Common;

/// <summary>
/// Tracks idempotency keys for commands to prevent duplicate processing.
/// Stores the serialised response so that replays can return the original result.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid IdempotencyKey { get; private set; }
    public string CommandName { get; private set; } = string.Empty;
    public string? ResponseJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(Guid idempotencyKey, string commandName, string? responseJson = null)
    {
        return new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            CommandName = commandName,
            ResponseJson = responseJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
