namespace MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

public sealed class InboxOptions
{
    public const string SectionName = "Messaging:Inbox";

    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 14;

    public int CleanupIntervalMinutes { get; set; } = 120;

    /// <summary>
    /// Soft lock duration while a handler runs. Expired locks can be taken over after a crash.
    /// </summary>
    public int LockDurationSeconds { get; set; } = 60;
}
