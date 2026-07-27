using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Single source of truth for broker names. Publishers, consumers and operators derive every queue
/// and exchange name from here instead of repeating literals.
/// </summary>
public sealed class MessagingTopology(IOptions<RabbitMqOptions> options, MessagingSource source)
{
    private readonly RabbitMqOptions _options = options.Value;

    public string Exchange => _options.Exchange;

    public string DeadLetterExchange => _options.DeadLetterExchange;

    public string QueueName => $"{source.ServiceName}.{_options.QueueSuffix}";

    public string RetryQueueName => $"{source.ServiceName}.retry";

    public string DeadLetterQueueName => $"{source.ServiceName}.{_options.DeadLetterQueueSuffix}";
}

public sealed record MessagingSource(string ServiceName);
