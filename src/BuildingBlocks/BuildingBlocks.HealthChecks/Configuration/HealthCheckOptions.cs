namespace MicroServiceSystem.BuildingBlocks.HealthChecks.Configuration;

public sealed class FrameworkHealthCheckOptions
{
    public const string SectionName = "HealthChecks";

    public string LivenessPath { get; set; } = "/health/live";

    public string ReadinessPath { get; set; } = "/health/ready";

    public string StartupPath { get; set; } = "/health/startup";

    public int TimeoutSeconds { get; set; } = 5;

    public bool ExposeDetailedResponse { get; set; }
}

public static class HealthCheckTags
{
    public const string Live = "live";

    public const string Ready = "ready";

    public const string Startup = "startup";

    public const string Database = "database";

    public const string Cache = "cache";

    public const string Broker = "broker";
}
