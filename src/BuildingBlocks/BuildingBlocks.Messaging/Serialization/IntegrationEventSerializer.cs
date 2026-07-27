using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Serialization;

public sealed class IntegrationEventSerializer : IIntegrationEventSerializer
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public IntegrationEventEnvelope Serialize(IIntegrationEvent integrationEvent, string source)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new IntegrationEventEnvelope
        {
            MessageId = integrationEvent.EventId,
            EventName = IntegrationEventNaming.Resolve(integrationEvent.GetType()),
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            TenantId = integrationEvent.TenantId,
            CorrelationId = integrationEvent.CorrelationId,
            TraceParent = Activity.Current?.Id,
            Source = source
        };
    }

    public IIntegrationEvent Deserialize(IntegrationEventEnvelope envelope, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(eventType);

        return JsonSerializer.Deserialize(envelope.Payload, eventType, SerializerOptions) as IIntegrationEvent
            ?? throw new InvalidOperationException(
                $"Message '{envelope.MessageId}' could not be deserialized into '{eventType.FullName}'.");
    }
}
