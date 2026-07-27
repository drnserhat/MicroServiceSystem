using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

public sealed class EfOutboxRepository<TContext>(TContext context, IDateTimeProvider dateTimeProvider)
    : IOutboxRepository
    where TContext : DbContext
{
    private static readonly ConcurrentDictionary<IModel, string> ClaimStatements = new();

    public async Task<IReadOnlyList<IntegrationEventEnvelope>> ClaimPendingAsync(
        int batchSize,
        TimeSpan leaseDuration,
        string workerId,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(leaseDuration.TotalSeconds);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTimeOffset leaseUntil = now.Add(leaseDuration);
        string sql = ClaimStatements.GetOrAdd(context.Model, BuildClaimStatement);

        // A single statement is atomic on its own, so the relay needs no user-initiated transaction.
        // That matters because every service enables EnableRetryOnFailure, and a retrying execution
        // strategy refuses transactions it did not start itself.
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            token => ClaimAsync(sql, batchSize, maxAttempts, now, leaseUntil, workerId, token),
            cancellationToken);
    }

    public async Task<bool> MarkPublishedAsync(
        Guid messageId,
        string workerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        DateTimeOffset now = dateTimeProvider.UtcNow;

        int updated = await context.Set<OutboxMessage>()
            .Where(message => message.Id == messageId && message.LockedBy == workerId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.ProcessedOnUtc, now)
                    .SetProperty(message => message.Error, (string?)null)
                    .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(message => message.LockedBy, (string?)null),
                cancellationToken);

        return updated > 0;
    }

    public async Task<OutboxFailureOutcome> MarkFailedAsync(
        Guid messageId,
        string workerId,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        string truncated = Truncate(error);
        DateTimeOffset now = dateTimeProvider.UtcNow;

        // Need the current attempt count under our lease before deciding whether this failure is terminal.
        // Two round trips are fine: claim already paid for the exclusive lock.
        int? currentAttempts = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(message => message.Id == messageId && message.LockedBy == workerId)
            .Select(message => (int?)message.AttemptCount)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentAttempts is null)
        {
            return OutboxFailureOutcome.LeaseLost;
        }

        int nextAttempts = currentAttempts.Value + 1;
        bool deadLetter = nextAttempts >= maxAttempts;

        int updated = deadLetter
            ? await context.Set<OutboxMessage>()
                .Where(message => message.Id == messageId && message.LockedBy == workerId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.AttemptCount, nextAttempts)
                        .SetProperty(message => message.Error, truncated)
                        .SetProperty(message => message.DeadLetteredOnUtc, now)
                        .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(message => message.LockedBy, (string?)null),
                    cancellationToken)
            : await context.Set<OutboxMessage>()
                .Where(message => message.Id == messageId && message.LockedBy == workerId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.AttemptCount, nextAttempts)
                        .SetProperty(message => message.Error, truncated)
                        .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(message => message.LockedBy, (string?)null),
                    cancellationToken);

        if (updated == 0)
        {
            return OutboxFailureOutcome.LeaseLost;
        }

        return deadLetter ? OutboxFailureOutcome.DeadLettered : OutboxFailureOutcome.Retried;
    }

    public Task<int> SealExhaustedAsync(int maxAttempts, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        DateTimeOffset now = dateTimeProvider.UtcNow;

        return context.Set<OutboxMessage>()
            .Where(message =>
                message.ProcessedOnUtc == null
                && message.DeadLetteredOnUtc == null
                && message.AttemptCount >= maxAttempts)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.DeadLetteredOnUtc, now)
                    .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(message => message.LockedBy, (string?)null)
                    .SetProperty(
                        message => message.Error,
                        message => message.Error ?? "Sealed as dead-lettered after exhausting publish attempts."),
                cancellationToken);
    }

    public Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken = default) =>
        context.Set<OutboxMessage>()
            .AsNoTracking()
            .CountAsync(message => message.DeadLetteredOnUtc != null && message.ProcessedOnUtc == null, cancellationToken);

    public Task<int> DeletePublishedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default) =>
        context.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc != null && message.ProcessedOnUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeleteDeadLetteredOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default) =>
        context.Set<OutboxMessage>()
            .Where(message =>
                message.DeadLetteredOnUtc != null
                && message.ProcessedOnUtc == null
                && message.DeadLetteredOnUtc < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);

    private async Task<IReadOnlyList<IntegrationEventEnvelope>> ClaimAsync(
        string sql,
        int batchSize,
        int maxAttempts,
        DateTimeOffset now,
        DateTimeOffset leaseUntil,
        string workerId,
        CancellationToken cancellationToken)
    {
        DbConnection connection = context.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;

            if (context.Database.CurrentTransaction is { } ambientTransaction)
            {
                command.Transaction = ambientTransaction.GetDbTransaction();
            }

            AddParameter(command, "lease_until", leaseUntil);
            AddParameter(command, "worker_id", workerId);
            AddParameter(command, "max_attempts", maxAttempts);
            AddParameter(command, "now", now);
            AddParameter(command, "batch_size", batchSize);

            var claimed = new List<IntegrationEventEnvelope>(batchSize);

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                claimed.Add(ReadEnvelope(reader));
            }

            return claimed;
        }
        finally
        {
            if (openedHere)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>
    /// Claims and returns the batch in one round trip. The inner select takes the row locks and the outer
    /// update stamps the lease, so no row can be handed to two relays even without an outer transaction.
    /// </summary>
    private static string BuildClaimStatement(IModel model)
    {
        IEntityType entityType = model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage is not mapped on the current DbContext.");

        StoreObjectIdentifier storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
            ?? throw new InvalidOperationException("OutboxMessage is not mapped to a table.");

        string table = Quote(entityType.GetTableName()
            ?? throw new InvalidOperationException("OutboxMessage has no table name."));
        string? schema = entityType.GetSchema();
        string qualified = schema is null ? table : $"{Quote(schema)}.{table}";

        string id = Column(entityType, storeObject, nameof(OutboxMessage.Id));
        string lockedUntil = Column(entityType, storeObject, nameof(OutboxMessage.LockedUntilUtc));
        string lockedBy = Column(entityType, storeObject, nameof(OutboxMessage.LockedBy));
        string processedOn = Column(entityType, storeObject, nameof(OutboxMessage.ProcessedOnUtc));
        string attemptCount = Column(entityType, storeObject, nameof(OutboxMessage.AttemptCount));
        string occurredOn = Column(entityType, storeObject, nameof(OutboxMessage.OccurredOnUtc));
        string deadLetteredOn = Column(entityType, storeObject, nameof(OutboxMessage.DeadLetteredOnUtc));

        string returning = string.Join(
            ", ",
            new[]
            {
                nameof(OutboxMessage.Id),
                nameof(OutboxMessage.EventName),
                nameof(OutboxMessage.Payload),
                nameof(OutboxMessage.OccurredOnUtc),
                nameof(OutboxMessage.TenantId),
                nameof(OutboxMessage.CorrelationId),
                nameof(OutboxMessage.TraceParent),
                nameof(OutboxMessage.Source),
                nameof(OutboxMessage.AttemptCount)
            }.Select(property => $"o.{Column(entityType, storeObject, property)}"));

        return $"""
            UPDATE {qualified} AS o
            SET {lockedUntil} = @lease_until, {lockedBy} = @worker_id
            WHERE o.{id} IN (
                SELECT c.{id}
                FROM {qualified} AS c
                WHERE c.{processedOn} IS NULL
                  AND c.{deadLetteredOn} IS NULL
                  AND c.{attemptCount} < @max_attempts
                  AND (c.{lockedUntil} IS NULL OR c.{lockedUntil} < @now)
                ORDER BY c.{occurredOn}
                LIMIT @batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING {returning}
            """;
    }

    private static string Column(IEntityType entityType, StoreObjectIdentifier storeObject, string propertyName)
    {
        IProperty property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"OutboxMessage.{propertyName} is not mapped.");

        return Quote(property.GetColumnName(storeObject)
            ?? throw new InvalidOperationException($"OutboxMessage.{propertyName} has no column name."));
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static IntegrationEventEnvelope ReadEnvelope(DbDataReader reader) =>
        new()
        {
            MessageId = reader.GetGuid(0),
            EventName = reader.GetString(1),
            Payload = reader.GetString(2),
            OccurredOnUtc = reader.GetFieldValue<DateTimeOffset>(3),
            TenantId = reader.IsDBNull(4) ? null : reader.GetGuid(4),
            CorrelationId = reader.IsDBNull(5) ? null : reader.GetString(5),
            TraceParent = reader.IsDBNull(6) ? null : reader.GetString(6),
            Source = reader.IsDBNull(7) ? null : reader.GetString(7),
            AttemptCount = reader.GetInt32(8)
        };

    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static string Truncate(string error) => error.Length <= 4000 ? error : error[..4000];
}
