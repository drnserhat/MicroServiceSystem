using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

public sealed class OutboxRelayHealthCheck(
    OutboxRelayDiagnostics diagnostics,
    IOptions<OutboxOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        OutboxRelaySnapshot snapshot = diagnostics.Snapshot();
        OutboxOptions settings = options.Value;

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["consecutiveFailures"] = snapshot.ConsecutiveFailures,
            ["deadLetterBacklog"] = snapshot.DeadLetterBacklog
        };

        if (snapshot.LastSuccessUtc is { } lastSuccess)
        {
            data["lastSuccessUtc"] = lastSuccess;
        }

        if (snapshot.LastFailureUtc is { } lastFailure)
        {
            data["lastFailureUtc"] = lastFailure;
        }

        if (snapshot.ConsecutiveFailures >= settings.UnhealthyAfterConsecutiveFailures)
        {
            HealthStatus failureStatus = context.Registration?.FailureStatus ?? HealthStatus.Unhealthy;

            return Task.FromResult(new HealthCheckResult(
                failureStatus,
                $"Outbox relay failed {snapshot.ConsecutiveFailures} consecutive cycles; no event is leaving this service.",
                data: data));
        }

        if (settings.DegradedAfterDeadLetterCount > 0
            && snapshot.DeadLetterBacklog >= settings.DegradedAfterDeadLetterCount)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Outbox has {snapshot.DeadLetterBacklog} dead-lettered message(s) waiting for operator attention.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Outbox relay is draining.", data));
    }
}
