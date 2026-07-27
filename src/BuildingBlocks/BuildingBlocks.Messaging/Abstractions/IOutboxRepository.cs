using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

public interface IOutboxRepository
{
    Task<IReadOnlyList<IntegrationEventEnvelope>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    Task<int> DeletePublishedOlderThanAsync(
        DateTimeOffset thresholdUtc,
        CancellationToken cancellationToken = default);
}
