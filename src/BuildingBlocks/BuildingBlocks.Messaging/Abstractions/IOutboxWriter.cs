using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

public interface IOutboxWriter
{
    Task AppendAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default);

    Task AppendRangeAsync(
        IEnumerable<IntegrationEventEnvelope> envelopes,
        CancellationToken cancellationToken = default);
}
