using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Inbox;

public sealed class EfInboxRepository<TContext>(TContext context, IDateTimeProvider dateTimeProvider)
    : IInboxRepository
    where TContext : DbContext
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AnyAsync(message => message.MessageId == messageId && message.ProcessedOnUtc != null, cancellationToken);

    public async Task MarkProcessedAsync(
        Guid messageId,
        string eventName,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;

        InboxMessage? existing = await context.Set<InboxMessage>()
            .FirstOrDefaultAsync(message => message.MessageId == messageId, cancellationToken);

        if (existing is null)
        {
            await context.Set<InboxMessage>().AddAsync(
                new InboxMessage
                {
                    MessageId = messageId,
                    EventName = eventName,
                    ReceivedOnUtc = now,
                    ProcessedOnUtc = now,
                    AttemptCount = 1
                },
                cancellationToken);
        }
        else
        {
            existing.ProcessedOnUtc = now;
            existing.AttemptCount += 1;
            existing.Error = null;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string eventName,
        string error,
        CancellationToken cancellationToken = default)
    {
        InboxMessage? existing = await context.Set<InboxMessage>()
            .FirstOrDefaultAsync(message => message.MessageId == messageId, cancellationToken);

        if (existing is null)
        {
            await context.Set<InboxMessage>().AddAsync(
                new InboxMessage
                {
                    MessageId = messageId,
                    EventName = eventName,
                    ReceivedOnUtc = dateTimeProvider.UtcNow,
                    AttemptCount = 1,
                    Error = Truncate(error)
                },
                cancellationToken);
        }
        else
        {
            existing.AttemptCount += 1;
            existing.Error = Truncate(error);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteProcessedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .Where(message => message.ProcessedOnUtc != null && message.ProcessedOnUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);

    private static string Truncate(string error) => error.Length <= 4000 ? error : error[..4000];
}
