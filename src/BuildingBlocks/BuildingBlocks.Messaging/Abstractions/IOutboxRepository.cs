using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

public interface IOutboxRepository
{
    /// <summary>
    /// Atomically claims a batch of unpublished messages using database row locks
    /// (<c>FOR UPDATE SKIP LOCKED</c>) so multiple relay instances do not publish duplicates.
    /// </summary>
    Task<IReadOnlyList<IntegrationEventEnvelope>> ClaimPendingAsync(
        int batchSize,
        TimeSpan leaseDuration,
        string workerId,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a claimed message. Returns <see langword="false"/> when the lease has already moved to
    /// another worker, which means this worker must not report the message as its own success.
    /// </summary>
    Task<bool> MarkPublishedAsync(Guid messageId, string workerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed publish attempt and releases the lease. When the next attempt count reaches
    /// <paramref name="maxAttempts"/> the row is dead-lettered so it stops being claimed and stays
    /// inspectable instead of becoming an invisible poison backlog.
    /// </summary>
    Task<OutboxFailureOutcome> MarkFailedAsync(
        Guid messageId,
        string workerId,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps <c>DeadLetteredOnUtc</c> on any unpublished row that already exhausted its attempts
    /// without being sealed — typically leftovers from before dead-lettering existed.
    /// </summary>
    Task<int> SealExhaustedAsync(int maxAttempts, CancellationToken cancellationToken = default);

    /// <summary>How many rows are parked as poison and waiting for operator attention.</summary>
    Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken = default);

    /// <summary>Unpublished rows that are still eligible for claim (not dead-lettered).</summary>
    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxDeadLetterRow>> ListDeadLetteredAsync(
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublished rows still eligible for claim (not dead-lettered). Metadata only — no payload.
    /// </summary>
    Task<IReadOnlyList<OutboxPendingRow>> ListPendingAsync(
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears dead-letter seal and resets attempts so the relay can claim the row again.
    /// </summary>
    Task<bool> RequeueDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<int> DeletePublishedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes dead-lettered rows older than <paramref name="thresholdUtc"/>. Kept separate from the
    /// published cleanup so poison history outlives successful deliveries by default.
    /// </summary>
    Task<int> DeleteDeadLetteredOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default);
}

public sealed record OutboxDeadLetterRow(
    Guid Id,
    string EventName,
    DateTimeOffset OccurredOnUtc,
    DateTimeOffset? DeadLetteredOnUtc,
    int AttemptCount,
    string? Error,
    Guid? TenantId,
    string? CorrelationId);

/// <summary>Pending outbox row metadata for ops surfaces — intentionally excludes Payload.</summary>
public sealed record OutboxPendingRow(
    Guid Id,
    string EventName,
    DateTimeOffset OccurredOnUtc,
    int AttemptCount,
    Guid? TenantId,
    string? CorrelationId,
    DateTimeOffset? LockedUntilUtc);

public enum OutboxFailureOutcome
{
    /// <summary>Another worker owns the lease; this worker must not act on the result.</summary>
    LeaseLost = 0,

    /// <summary>Attempt recorded; the row remains eligible for a later claim.</summary>
    Retried = 1,

    /// <summary>Attempts exhausted; the row is sealed and will not be claimed again.</summary>
    DeadLettered = 2
}
