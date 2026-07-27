using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Application port for emitting integration events. The implementation writes to the outbox inside
/// the current transaction, so state changes and published messages can never diverge.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default);
}
