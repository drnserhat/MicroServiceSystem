using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher messagePublisher,
    IOptions<OutboxOptions> options,
    OutboxRelayDiagnostics diagnostics,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxOptions outboxOptions = options.Value;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(outboxOptions.PollingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(outboxOptions, stoppingToken);
                diagnostics.RecordSuccess(dateTimeProvider.UtcNow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                diagnostics.RecordFailure(dateTimeProvider.UtcNow, exception.Message);

                logger.LogError(
                    exception,
                    "Outbox processing cycle failed ({ConsecutiveFailures} in a row); no event is leaving this service",
                    diagnostics.ConsecutiveFailures);
            }
        }
    }

    private async Task ProcessBatchAsync(OutboxOptions outboxOptions, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IOutboxRepository repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        // Seal leftovers that already hit MaxAttempts before dead-lettering existed, so the health check
        // can see them instead of leaving them as an invisible attempt_count backlog.
        int sealedCount = await repository.SealExhaustedAsync(outboxOptions.MaxAttempts, cancellationToken);
        if (sealedCount > 0)
        {
            logger.LogWarning(
                "Sealed {Count} exhausted outbox message(s) as dead-lettered",
                sealedCount);
        }

        IReadOnlyList<IntegrationEventEnvelope> pending = await repository.ClaimPendingAsync(
            outboxOptions.BatchSize,
            TimeSpan.FromSeconds(outboxOptions.LockDurationSeconds),
            _workerId,
            outboxOptions.MaxAttempts,
            cancellationToken);

        foreach (IntegrationEventEnvelope envelope in pending)
        {
            try
            {
                await messagePublisher.PublishAsync(envelope, cancellationToken);

                if (!await repository.MarkPublishedAsync(envelope.MessageId, _workerId, cancellationToken))
                {
                    logger.LogWarning(
                        "Lease on outbox message {MessageId} expired before the publish completed; another relay may republish it",
                        envelope.MessageId);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                OutboxFailureOutcome outcome = await repository.MarkFailedAsync(
                    envelope.MessageId,
                    _workerId,
                    exception.Message,
                    outboxOptions.MaxAttempts,
                    cancellationToken);

                if (outcome == OutboxFailureOutcome.DeadLettered)
                {
                    logger.LogError(
                        exception,
                        "Outbox message {MessageId} of type {EventName} dead-lettered after exhausting publish attempts",
                        envelope.MessageId,
                        envelope.EventName);
                }
                else
                {
                    logger.LogError(
                        exception,
                        "Publishing outbox message {MessageId} of type {EventName} failed",
                        envelope.MessageId,
                        envelope.EventName);
                }
            }
        }

        diagnostics.RecordDeadLetterBacklog(
            await repository.CountDeadLetteredAsync(cancellationToken));
    }
}
