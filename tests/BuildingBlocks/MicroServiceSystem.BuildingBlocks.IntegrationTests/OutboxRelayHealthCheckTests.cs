using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Outbox;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class OutboxRelayHealthCheckTests
{
    [Fact]
    public async Task Healthy_when_relay_is_draining_and_there_is_no_poison_backlog()
    {
        var diagnostics = new OutboxRelayDiagnostics();
        diagnostics.RecordSuccess(DateTimeOffset.UtcNow);
        diagnostics.RecordDeadLetterBacklog(0);

        HealthCheckResult result = await CreateCheck(diagnostics).CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Degraded_when_dead_lettered_messages_are_waiting_for_attention()
    {
        var diagnostics = new OutboxRelayDiagnostics();
        diagnostics.RecordSuccess(DateTimeOffset.UtcNow);
        diagnostics.RecordDeadLetterBacklog(3);

        HealthCheckResult result = await CreateCheck(diagnostics).CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("3");
        result.Data["deadLetterBacklog"].ShouldBe(3);
    }

    [Fact]
    public async Task Unhealthy_when_relay_cycles_keep_failing_even_if_poison_is_also_present()
    {
        var diagnostics = new OutboxRelayDiagnostics();
        diagnostics.RecordFailure(DateTimeOffset.UtcNow, "broker down");
        diagnostics.RecordFailure(DateTimeOffset.UtcNow, "broker down");
        diagnostics.RecordFailure(DateTimeOffset.UtcNow, "broker down");
        diagnostics.RecordDeadLetterBacklog(2);

        HealthCheckResult result = await CreateCheck(diagnostics).CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        // Cycle failure is worse than a parked poison backlog: nothing is leaving the service at all.
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["consecutiveFailures"].ShouldBe(3);
        result.Data["deadLetterBacklog"].ShouldBe(2);
    }

    private static OutboxRelayHealthCheck CreateCheck(OutboxRelayDiagnostics diagnostics) =>
        new(
            diagnostics,
            Options.Create(new OutboxOptions
            {
                UnhealthyAfterConsecutiveFailures = 3,
                DegradedAfterDeadLetterCount = 1
            }));
}
