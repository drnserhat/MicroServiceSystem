namespace MicroServiceSystem.BuildingBlocks.Persistence.Inbox;

/// <summary>
/// De-duplication / reservation record for consumed messages.
/// </summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateTimeOffset ReceivedOnUtc { get; set; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// Soft lock while a handler is in flight. Expired locks can be taken over after a crash.
    /// </summary>
    public DateTimeOffset? LockedUntilUtc { get; set; }
}
