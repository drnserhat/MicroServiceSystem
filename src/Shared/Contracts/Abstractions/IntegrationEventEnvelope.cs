using System.Text.Json.Serialization;

namespace MicroServiceSystem.Contracts.Abstractions;

/// <summary>
/// Transport representation of an integration event. Producers and consumers exchange this shape so
/// that headers survive Outbox persistence, broker delivery and Inbox de-duplication.
/// </summary>
public sealed record IntegrationEventEnvelope
{
    public required Guid MessageId { get; init; }

    public required string EventName { get; init; }

    [JsonConverter(typeof(RawJsonStringConverter))]
    public required string Payload { get; init; }

    public required DateTimeOffset OccurredOnUtc { get; init; }

    public Guid? TenantId { get; init; }

    public string? CorrelationId { get; init; }

    public string? TraceParent { get; init; }

    public string? Source { get; init; }

    public int AttemptCount { get; init; }
}
