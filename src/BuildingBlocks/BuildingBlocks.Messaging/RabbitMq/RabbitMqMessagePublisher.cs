using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.Contracts.Abstractions;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqMessagePublisher(
    RabbitMqConnectionProvider connectionProvider,
    MessagingTopology topology,
    IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    public const string TenantHeader = "x-tenant-id";
    public const string CorrelationHeader = "x-correlation-id";
    public const string AttemptHeader = "x-attempt-count";
    public const string SourceHeader = "x-source";

    public async Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        RabbitMqOptions rabbitOptions = options.Value;

        await using IChannel channel = await connectionProvider.CreateChannelAsync(
            rabbitOptions.PublisherConfirms,
            cancellationToken);

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

        byte[] body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(envelope, IntegrationEventSerializer.SerializerOptions));

        await channel.BasicPublishAsync(
            topology.Exchange,
            envelope.EventName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
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
