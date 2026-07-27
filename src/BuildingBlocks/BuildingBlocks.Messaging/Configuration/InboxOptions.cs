namespace MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

public sealed class InboxOptions
{
    public const string SectionName = "Messaging:Inbox";

    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 14;

    public int CleanupIntervalMinutes { get; set; } = 120;
}
