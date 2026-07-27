using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

public sealed class EfOutboxRepository<TContext>(TContext context, IDateTimeProvider dateTimeProvider)
    : IOutboxRepository
    where TContext : DbContext
{
    public async Task<IReadOnlyList<IntegrationEventEnvelope>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        List<OutboxMessage> messages = await context.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages.ConvertAll(ToEnvelope);
    }

    public async Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        await context.Set<OutboxMessage>()
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.ProcessedOnUtc, dateTimeProvider.UtcNow)
                    .SetProperty(message => message.Error, (string?)null),
                cancellationToken);

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default) =>
        await context.Set<OutboxMessage>()
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                    .SetProperty(message => message.Error, Truncate(error)),
                cancellationToken);

    public Task<int> DeletePublishedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default) =>
        context.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc != null && message.ProcessedOnUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);

    private static string Truncate(string error) => error.Length <= 4000 ? error : error[..4000];

    private static IntegrationEventEnvelope ToEnvelope(OutboxMessage message) =>
        new()
        {
            MessageId = message.Id,
            EventName = message.EventName,
            Payload = message.Payload,
            OccurredOnUtc = message.OccurredOnUtc,
            TenantId = message.TenantId,
            CorrelationId = message.CorrelationId,
            TraceParent = message.TraceParent,
            Source = message.Source,
            AttemptCount = message.AttemptCount
        };
}
