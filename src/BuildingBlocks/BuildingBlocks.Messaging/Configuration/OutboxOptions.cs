namespace MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 100;

    public int MaxAttempts { get; set; } = 10;

    public int RetentionDays { get; set; } = 7;

    public int CleanupIntervalMinutes { get; set; } = 60;
}
