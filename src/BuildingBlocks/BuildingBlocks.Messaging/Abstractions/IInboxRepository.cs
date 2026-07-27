namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// De-duplication store for at-least-once delivery. A consumer only executes when the message id was
/// not processed before, which makes every handler effectively idempotent.
/// </summary>
public interface IInboxRepository
{
    Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid messageId, string eventName, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string eventName, string error, CancellationToken cancellationToken = default);

    Task<int> DeleteProcessedOlderThanAsync(DateTimeOffset thresholdUtc, CancellationToken cancellationToken = default);
}
