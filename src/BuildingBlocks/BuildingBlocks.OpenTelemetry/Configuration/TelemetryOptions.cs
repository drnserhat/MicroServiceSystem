namespace MicroServiceSystem.BuildingBlocks.OpenTelemetry.Configuration;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = "1.0.0";

    public bool TracingEnabled { get; set; } = true;

    public bool MetricsEnabled { get; set; } = true;

    public string OtlpEndpoint { get; set; } = string.Empty;

    public bool PrometheusScrapingEnabled { get; set; } = true;

    public string PrometheusScrapingPath { get; set; } = "/metrics";

    public double TraceSamplingRatio { get; set; } = 1.0;
}
