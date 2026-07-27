using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

public interface IIntegrationEventSerializer
{
    IntegrationEventEnvelope Serialize(IIntegrationEvent integrationEvent, string source);

    IIntegrationEvent Deserialize(IntegrationEventEnvelope envelope, Type eventType);
}
