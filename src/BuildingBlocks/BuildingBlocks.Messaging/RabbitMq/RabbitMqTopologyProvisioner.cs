using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqTopologyProvisioner(
    RabbitMqConnectionProvider connectionProvider,
    MessagingTopology topology,
    IIntegrationEventRegistry registry,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqTopologyProvisioner> logger)
{
    private const string DeadLetterExchangeArgument = "x-dead-letter-exchange";
    private const string DeadLetterRoutingKeyArgument = "x-dead-letter-routing-key";
    private const string MessageTtlArgument = "x-message-ttl";
    private const string QueueTypeArgument = "x-queue-type";
    private const string QuorumQueueType = "quorum";

    public async Task ProvisionAsync(bool declareConsumerQueues, CancellationToken cancellationToken = default)
    {
        RabbitMqOptions rabbitOptions = options.Value;

        await using IChannel channel = await connectionProvider.CreateChannelAsync(
            publisherConfirms: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            topology.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            topology.DeadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        if (!declareConsumerQueues)
        {
            return;
        }

        await channel.QueueDeclareAsync(
            topology.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                [QueueTypeArgument] = QuorumQueueType,
                [DeadLetterExchangeArgument] = topology.DeadLetterExchange,
                [DeadLetterRoutingKeyArgument] = topology.DeadLetterQueueName
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            topology.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                [QueueTypeArgument] = QuorumQueueType,
                [MessageTtlArgument] = rabbitOptions.RetryBaseDelaySeconds * 1000,
                [DeadLetterExchangeArgument] = string.Empty,
                [DeadLetterRoutingKeyArgument] = topology.QueueName
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            topology.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { [QueueTypeArgument] = QuorumQueueType },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            topology.DeadLetterQueueName,
            topology.DeadLetterExchange,
            topology.DeadLetterQueueName,
            cancellationToken: cancellationToken);

        foreach (string eventName in registry.SubscribedEventNames)
        {
            await channel.QueueBindAsync(
                topology.QueueName,
                topology.Exchange,
                eventName,
                cancellationToken: cancellationToken);

            logger.LogInformation("Bound {Queue} to {Exchange} with routing key {RoutingKey}",
                topology.QueueName,
                topology.Exchange,
                eventName);
        }
    }
}
