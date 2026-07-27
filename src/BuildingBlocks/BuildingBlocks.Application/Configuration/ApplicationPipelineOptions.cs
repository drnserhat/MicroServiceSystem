namespace MicroServiceSystem.BuildingBlocks.Application.Configuration;

public sealed class ApplicationPipelineOptions
{
    public const string SectionName = "Application:Pipeline";

    public int SlowRequestThresholdMilliseconds { get; set; } = 500;

    public bool EnableRequestLogging { get; set; } = true;

    public bool EnablePerformanceLogging { get; set; } = true;

    public bool EnableUnitOfWorkBehavior { get; set; } = true;
}
