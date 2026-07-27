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
    RabbitMqChannelPool channelPool,
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
        ushort dispatchConcurrency = (ushort)Math.Clamp(rabbitOptions.ConsumerConcurrency, 1, ushort.MaxValue);

        // Without an explicit dispatch concurrency the client hands deliveries to the handler one at a
        // time, so prefetch alone would only buffer messages instead of processing them in parallel.
        _channel = await connectionProvider.CreateChannelAsync(
            publisherConfirms: false,
            consumerDispatchConcurrency: dispatchConcurrency,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, rabbitOptions.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, deliveryArguments) =>
            await HandleDeliveryAsync(deliveryArguments, rabbitOptions, stoppingToken);

        await _channel.BasicConsumeAsync(topology.QueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation(
            "Consuming {Queue} with prefetch {Prefetch} and dispatch concurrency {Concurrency}",
            topology.QueueName,
            rabbitOptions.PrefetchCount,
            dispatchConcurrency);

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
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
                deliveryArguments.Body.Span,
                IntegrationEventSerializer.SerializerOptions);

            if (envelope is null)
            {
                await MoveToDeadLetterAsync(deliveryArguments, "Envelope could not be deserialized.", cancellationToken);
                return;
            }

            DispatchOutcome outcome = await dispatcher.DispatchAsync(envelope, cancellationToken);

            if (outcome == DispatchOutcome.Contended)
            {
                await RescheduleContendedAsync(deliveryArguments, envelope, cancellationToken);
                return;
            }

            await _channel!.BasicAckAsync(deliveryArguments.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Handling message {MessageId} failed",
                envelope?.MessageId.ToString() ?? deliveryArguments.BasicProperties.MessageId);

            await RetryOrDeadLetterAsync(deliveryArguments, envelope, rabbitOptions, exception, cancellationToken);
        }
    }

    /// <summary>
    /// Another consumer holds the inbox lease. Requeuing straight away would spin against the broker and
    /// the database until that lease expires, so the delivery goes through the delayed retry queue instead
    /// and the attempt counter is left untouched.
    /// </summary>
    private async Task RescheduleContendedAsync(
        BasicDeliverEventArgs deliveryArguments,
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Message {MessageId} is reserved by another consumer, rescheduling through {RetryQueue}",
            envelope.MessageId,
            topology.RetryQueueName);

        await PublishToRetryQueueAsync(envelope, cancellationToken);
        await _channel!.BasicAckAsync(deliveryArguments.DeliveryTag, multiple: false, cancellationToken);
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

        await PublishToRetryQueueAsync(envelope with { AttemptCount = attemptCount }, cancellationToken);
        await _channel!.BasicAckAsync(deliveryArguments.DeliveryTag, multiple: false, cancellationToken);

        logger.LogWarning(
            "Message {MessageId} scheduled for retry {AttemptCount}/{MaxAttempts}",
            envelope.MessageId,
            attemptCount,
            rabbitOptions.MaxDeliveryAttempts);
    }

    /// <summary>
    /// Uses a confirmed publisher channel: the original delivery is only acked once the broker has
    /// accepted the retry copy, otherwise a lost republish would silently drop the message.
    /// </summary>
    private async Task PublishToRetryQueueAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(
            envelope,
            IntegrationEventSerializer.SerializerOptions);

        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId.ToString(),
            Type = envelope.EventName,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        IChannel channel = await channelPool.RentAsync(cancellationToken);

        try
        {
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: topology.RetryQueueName,
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
