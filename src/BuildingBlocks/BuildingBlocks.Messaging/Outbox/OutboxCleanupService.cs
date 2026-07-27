using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

public sealed class OutboxCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> outboxOptions,
    IOptions<InboxOptions> inboxOptions,
    ILogger<OutboxCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(outboxOptions.Value.CleanupIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

                IOutboxRepository outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                int removedMessages = await outbox.DeletePublishedOlderThanAsync(
                    DateTimeOffset.UtcNow.AddDays(-outboxOptions.Value.RetentionDays),
                    stoppingToken);

                IInboxRepository? inbox = scope.ServiceProvider.GetService<IInboxRepository>();

                int removedInboxEntries = inbox is null
                    ? 0
                    : await inbox.DeleteProcessedOlderThanAsync(
                        DateTimeOffset.UtcNow.AddDays(-inboxOptions.Value.RetentionDays),
                        stoppingToken);

                logger.LogInformation(
                    "Message store cleanup removed {OutboxCount} outbox and {InboxCount} inbox records",
                    removedMessages,
                    removedInboxEntries);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Message store cleanup failed");
            }
        }
    }
}
