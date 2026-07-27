using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.Contracts.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Consumers;

public sealed class RabbitMqConsumerService(
    RabbitMqConnectionProvider connectionProvider,
    RabbitMqTopologyProvisioner topologyProvisioner,
    MessagingTopology topology,
    IntegrationEventDispatcher dispatcher,
    IIntegrationEventRegistry registry,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (registry.SubscribedEventNames.Count == 0)
        {
            logger.LogInformation("No integration event handlers are registered, consumer is idle");
            return;
        }

        await topologyProvisioner.ProvisionAsync(declareConsumerQueues: true, stoppingToken);

        RabbitMqOptions rabbitOptions = options.Value;

        _channel = await connectionProvider.CreateChannelAsync(false, stoppingToken);

        await _channel.BasicQosAsync(0, rabbitOptions.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, deliveryArguments) =>
            await HandleDeliveryAsync(deliveryArguments, rabbitOptions, stoppingToken);

        await _channel.BasicConsumeAsync(topology.QueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consuming {Queue} with prefetch {Prefetch}", topology.QueueName, rabbitOptions.PrefetchCount);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Consumer for {Queue} is shutting down", topology.QueueName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
            _channel = null;
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        BasicDeliverEventArgs deliveryArguments,
        RabbitMqOptions rabbitOptions,
        CancellationToken cancellationToken)
    {
        IntegrationEventEnvelope? envelope = null;

        try
        {
            string payload = Encoding.UTF8.GetString(deliveryArguments.Body.Span);
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
                payload,
                IntegrationEventSerializer.SerializerOptions);

            if (envelope is null)
            {
                await MoveToDeadLetterAsync(deliveryArguments, "Envelope could not be deserialized.", cancellationToken);
                return;
            }

            await dispatcher.DispatchAsync(envelope, cancellationToken);
            await _channel!.BasicAckAsync(deliveryArguments.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Handling message {MessageId} failed",
                envelope?.MessageId.ToString() ?? deliveryArguments.BasicProperties.MessageId);

            await RetryOrDeadLetterAsync(deliveryArguments, envelope, rabbitOptions, exception, cancellationToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        BasicDeliverEventArgs deliveryArguments,
        IntegrationEventEnvelope? envelope,
        RabbitMqOptions rabbitOptions,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int attemptCount = (envelope?.AttemptCount ?? 0) + 1;

        if (envelope is null || attemptCount >= rabbitOptions.MaxDeliveryAttempts)
        {
            await MoveToDeadLetterAsync(deliveryArguments, exception.Message, cancellationToken);
            return;
        }

        IntegrationEventEnvelope retryEnvelope = envelope with { AttemptCount = attemptCount };

        byte[] body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(retryEnvelope, IntegrationEventSerializer.SerializerOptions));

        var properties = new BasicProperties
        {
            MessageId = retryEnvelope.MessageId.ToString(),
            Type = retryEnvelope.EventName,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: topology.RetryQueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        await _channel.BasicAckAsync(deliveryArguments.DeliveryTag, multiple: false, cancellationToken);

        logger.LogWarning(
            "Message {MessageId} scheduled for retry {AttemptCount}/{MaxAttempts}",
            retryEnvelope.MessageId,
            attemptCount,
            rabbitOptions.MaxDeliveryAttempts);
    }

    private async Task MoveToDeadLetterAsync(
        BasicDeliverEventArgs deliveryArguments,
        string reason,
        CancellationToken cancellationToken)
    {
        logger.LogError("Message moved to dead letter queue {Queue}: {Reason}", topology.DeadLetterQueueName, reason);

        await _channel!.BasicNackAsync(
            deliveryArguments.DeliveryTag,
            multiple: false,
            requeue: false,
            cancellationToken);
    }
}
