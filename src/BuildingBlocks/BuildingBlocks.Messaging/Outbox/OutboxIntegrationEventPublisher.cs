using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Application facing publisher. It only appends to the outbox inside the caller transaction; the
/// outbox processor is what talks to the broker.
/// </summary>
public sealed class OutboxIntegrationEventPublisher(
    IOutboxWriter outboxWriter,
    IIntegrationEventSerializer serializer,
    ICurrentTenant currentTenant,
    MessagingSource source) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent =>
        outboxWriter.AppendAsync(CreateEnvelope(integrationEvent), cancellationToken);

    public Task PublishAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default) =>
        outboxWriter.AppendRangeAsync(integrationEvents.Select(CreateEnvelope), cancellationToken);

    private IntegrationEventEnvelope CreateEnvelope(IIntegrationEvent integrationEvent)
    {
        IntegrationEventEnvelope envelope = serializer.Serialize(integrationEvent, source.ServiceName);

        return envelope.TenantId is null && currentTenant.Id is { } tenantId
            ? envelope with { TenantId = tenantId }
            : envelope;
    }
}
