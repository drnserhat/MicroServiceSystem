using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    /// <summary>
    /// Topic exchange every service publishes to. Consumers bind their own queues with routing keys
    /// derived from integration event names.
    /// </summary>
    public string Exchange { get; set; } = "microservicesystem.events";

    public string DeadLetterExchange { get; set; } = "microservicesystem.events.dlx";

    public string QueueSuffix { get; set; } = "queue";

    public string DeadLetterQueueSuffix { get; set; } = "dlq";

    public ushort PrefetchCount { get; set; } = 16;

    public int ConsumerConcurrency { get; set; } = 4;

    public int MaxDeliveryAttempts { get; set; } = 5;

    public int RetryBaseDelaySeconds { get; set; } = 5;

    public bool PublisherConfirms { get; set; } = true;

    /// <summary>
    /// Channels kept alive for publishing. Opening a channel is a broker round trip, so the relay reuses
    /// them instead of paying that cost per message.
    /// </summary>
    public int PublisherChannelPoolSize { get; set; } = 8;

    public int ConnectionRetryCount { get; set; } = 10;

    public int ConnectionRetryDelaySeconds { get; set; } = 3;
}
