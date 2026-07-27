namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Row shape of the transactional outbox. It lives in the service database so an event is committed
/// together with the state change that produced it.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; set; }

    public Guid? TenantId { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceParent { get; set; }

    public string? Source { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// Set when the relay stops retrying this row. A dead-lettered message is never claimed again;
    /// it stays visible so operators can inspect <see cref="Error"/> instead of vanishing into a
    /// silent backlog of rows that only <c>attempt_count &gt;= max</c> would have excluded.
    /// </summary>
    public DateTimeOffset? DeadLetteredOnUtc { get; set; }

    /// <summary>Exclusive lease end for multi-instance outbox relays.</summary>
    public DateTimeOffset? LockedUntilUtc { get; set; }

    /// <summary>Worker that currently holds the lease.</summary>
    public string? LockedBy { get; set; }
}
