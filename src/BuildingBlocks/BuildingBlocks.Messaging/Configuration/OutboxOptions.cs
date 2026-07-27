namespace MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 100;

    public int MaxAttempts { get; set; } = 10;

    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// How long dead-lettered rows are kept for inspection. Longer than <see cref="RetentionDays"/>
    /// on purpose — poison history is more valuable than successful-delivery history.
    /// </summary>
    public int DeadLetterRetentionDays { get; set; } = 30;

    public int CleanupIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// How long a claimed outbox row stays leased to one relay worker.
    /// Expired leases can be reclaimed after a crash.
    /// </summary>
    public int LockDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Consecutive failed relay cycles before the outbox health check reports unhealthy.
    /// </summary>
    public int UnhealthyAfterConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Open dead-letter count at or above this value makes the outbox health check report Degraded.
    /// Zero disables the backlog signal.
    /// </summary>
    public int DegradedAfterDeadLetterCount { get; set; } = 1;
}
