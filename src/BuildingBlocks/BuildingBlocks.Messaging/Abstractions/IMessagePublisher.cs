using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Broker level publishing. Application code never calls this directly; the outbox processor does,
/// which is what keeps state changes and published messages consistent.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default);
}
