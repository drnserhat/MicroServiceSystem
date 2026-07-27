namespace MicroServiceSystem.BuildingBlocks.Resilience.Configuration;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public int TotalRequestTimeoutSeconds { get; set; } = 30;

    public int AttemptTimeoutSeconds { get; set; } = 10;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryBaseDelayMilliseconds { get; set; } = 200;

    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;

    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 15;
}
