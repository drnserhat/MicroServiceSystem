namespace MicroServiceSystem.BuildingBlocks.Persistence.Inbox;

/// <summary>
/// De-duplication record for consumed messages. Presence of a processed row is what makes a handler
/// safe under at-least-once delivery.
/// </summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateTimeOffset ReceivedOnUtc { get; set; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? Error { get; set; }
}
