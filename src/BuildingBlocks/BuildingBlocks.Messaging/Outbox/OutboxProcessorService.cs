using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher messagePublisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxOptions outboxOptions = options.Value;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(outboxOptions.PollingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(outboxOptions.BatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox processing cycle failed");
            }
        }
    }

    private async Task ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IOutboxRepository repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        IReadOnlyList<IntegrationEventEnvelope> pending =
            await repository.FetchPendingAsync(batchSize, cancellationToken);

        foreach (IntegrationEventEnvelope envelope in pending)
        {
            try
            {
                await messagePublisher.PublishAsync(envelope, cancellationToken);
                await repository.MarkPublishedAsync(envelope.MessageId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Publishing outbox message {MessageId} of type {EventName} failed",
                    envelope.MessageId,
                    envelope.EventName);

                await repository.MarkFailedAsync(envelope.MessageId, exception.Message, cancellationToken);
            }
        }
    }
}
