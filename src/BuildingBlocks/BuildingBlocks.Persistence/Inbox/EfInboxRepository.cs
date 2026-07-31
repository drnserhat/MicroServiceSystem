using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;
using Npgsql;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Inbox;

public sealed class EfInboxRepository<TContext>(TContext context, IDateTimeProvider dateTimeProvider)
    : IInboxRepository
    where TContext : DbContext
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AnyAsync(message => message.MessageId == messageId && message.ProcessedOnUtc != null, cancellationToken);

    public async Task<InboxReservationStatus> TryReserveAsync(
        Guid messageId,
        string eventName,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(leaseDuration.TotalSeconds);

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTimeOffset leaseUntil = now.Add(leaseDuration);

        DetachLocal(messageId);

        InboxMessage? existing = await context.Set<InboxMessage>()
            .AsNoTracking()
            .FirstOrDefaultAsync(message => message.MessageId == messageId, cancellationToken);

        if (existing is null)
        {
            try
            {
                await context.Set<InboxMessage>().AddAsync(
                    new InboxMessage
                    {
                        MessageId = messageId,
                        EventName = eventName,
                        ReceivedOnUtc = now,
                        AttemptCount = 0,
                        LockedUntilUtc = leaseUntil
                    },
                    cancellationToken);

                await context.SaveChangesAsync(cancellationToken);
                return InboxReservationStatus.Reserved;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                DetachLocal(messageId);

                existing = await context.Set<InboxMessage>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(message => message.MessageId == messageId, cancellationToken);

                if (existing is null)
                {
                    return InboxReservationStatus.Contended;
                }
            }
        }

        if (existing.ProcessedOnUtc is not null)
        {
            return InboxReservationStatus.Duplicate;
        }

        if (existing.LockedUntilUtc is not null && existing.LockedUntilUtc > now)
        {
            return InboxReservationStatus.Contended;
        }

        int taken = await context.Set<InboxMessage>()
            .Where(message =>
                message.MessageId == messageId
                && message.ProcessedOnUtc == null
                && (message.LockedUntilUtc == null || message.LockedUntilUtc < now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.LockedUntilUtc, leaseUntil)
                    .SetProperty(message => message.EventName, eventName),
                cancellationToken);

        return taken > 0 ? InboxReservationStatus.Reserved : InboxReservationStatus.Contended;
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        string eventName,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;
        DetachLocal(messageId);

        int updated = await context.Set<InboxMessage>()
            .Where(message => message.MessageId == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.ProcessedOnUtc, now)
                    .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(message => message.Error, (string?)null)
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                    .SetProperty(message => message.EventName, eventName),
                cancellationToken);

        if (updated > 0)
        {
            return;
        }

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

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string eventName,
        string error,
        CancellationToken cancellationToken = default)
    {
        string truncated = Truncate(error);
        DetachLocal(messageId);

        int updated = await context.Set<InboxMessage>()
            .Where(message => message.MessageId == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                    .SetProperty(message => message.Error, truncated)
                    .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(message => message.EventName, eventName),
                cancellationToken);

        if (updated > 0)
        {
            return;
        }

        await context.Set<InboxMessage>().AddAsync(
            new InboxMessage
            {
                MessageId = messageId,
                EventName = eventName,
                ReceivedOnUtc = dateTimeProvider.UtcNow,
                AttemptCount = 1,
                Error = truncated
            },
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteProcessedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .Where(message => message.ProcessedOnUtc != null && message.ProcessedOnUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountProcessedAsync(CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .CountAsync(message => message.ProcessedOnUtc != null, cancellationToken);

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .CountAsync(message => message.ProcessedOnUtc == null, cancellationToken);

    public Task<int> CountInFlightAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .CountAsync(
                message =>
                    message.ProcessedOnUtc == null
                    && message.LockedUntilUtc != null
                    && message.LockedUntilUtc > utcNow,
                cancellationToken);

    public Task<int> CountFailedAsync(CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .CountAsync(
                message => message.ProcessedOnUtc == null && message.Error != null,
                cancellationToken);

    private void DetachLocal(Guid messageId)
    {
        EntityEntry<InboxMessage>? tracked = context.ChangeTracker
            .Entries<InboxMessage>()
            .FirstOrDefault(entry => entry.Entity.MessageId == messageId);

        if (tracked is not null)
        {
            tracked.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static string Truncate(string error) => error.Length <= 4000 ? error : error[..4000];
}
