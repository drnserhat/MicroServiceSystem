using System.Text.Json;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.Contracts.Abstractions;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqMessagePublisher(
    RabbitMqChannelPool channelPool,
    MessagingTopology topology) : IMessagePublisher
{
    public const string TenantHeader = "x-tenant-id";
    public const string CorrelationHeader = "x-correlation-id";
    public const string AttemptHeader = "x-attempt-count";
    public const string SourceHeader = "x-source";

    public async Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId.ToString(),
            Type = envelope.EventName,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(envelope.OccurredOnUtc.ToUnixTimeSeconds()),
            CorrelationId = envelope.CorrelationId,
            Headers = BuildHeaders(envelope)
        };

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(
            envelope,
            IntegrationEventSerializer.SerializerOptions);

        IChannel channel = await channelPool.RentAsync(cancellationToken);

        try
        {
            await channel.BasicPublishAsync(
                topology.Exchange,
                envelope.EventName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            await channelPool.ReturnAsync(channel);
        }
    }

    private static Dictionary<string, object?> BuildHeaders(IntegrationEventEnvelope envelope)
    {
        var headers = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [AttemptHeader] = envelope.AttemptCount
        };

        if (envelope.TenantId is { } tenantId)
        {
            headers[TenantHeader] = tenantId.ToString();
        }

        if (!string.IsNullOrWhiteSpace(envelope.CorrelationId))
        {
            headers[CorrelationHeader] = envelope.CorrelationId;
        }

        if (!string.IsNullOrWhiteSpace(envelope.Source))
        {
            headers[SourceHeader] = envelope.Source;
        }

        return headers;
    }
}
