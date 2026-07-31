namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// De-duplication store for at-least-once delivery. Consumers <see cref="TryReserveAsync"/> before
/// running handlers so concurrent deliveries of the same message id cannot both execute side effects.
/// </summary>
public interface IInboxRepository
{
    Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to reserve <paramref name="messageId"/> for exclusive processing.
    /// </summary>
    Task<InboxReservationStatus> TryReserveAsync(
        Guid messageId,
        string eventName,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid messageId, string eventName, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string eventName, string error, CancellationToken cancellationToken = default);

    Task<int> DeleteProcessedOlderThanAsync(DateTimeOffset thresholdUtc, CancellationToken cancellationToken = default);

    /// <summary>Ops aggregate: successfully processed inbox rows.</summary>
    Task<int> CountProcessedAsync(CancellationToken cancellationToken = default);

    /// <summary>Ops aggregate: not yet processed (open + in-flight + failed).</summary>
    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Ops aggregate: open rows currently under a fresh lease.</summary>
    Task<int> CountInFlightAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);

    /// <summary>Ops aggregate: open rows with an error stamped (failed attempts).</summary>
    Task<int> CountFailedAsync(CancellationToken cancellationToken = default);
}

public enum InboxReservationStatus
{
    /// <summary>This worker owns the message and should run handlers.</summary>
    Reserved = 0,

    /// <summary>Message was already processed successfully.</summary>
    Duplicate = 1,

    /// <summary>Another worker holds a fresh reservation; retry later.</summary>
    Contended = 2
}
