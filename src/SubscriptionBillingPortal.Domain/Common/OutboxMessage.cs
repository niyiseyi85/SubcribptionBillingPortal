namespace SubscriptionBillingPortal.Domain.Common;

/// <summary>
/// Represents a domain event stored in the outbox for reliable async processing.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public bool IsProcessed => ProcessedAt.HasValue;

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
