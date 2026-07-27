namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Row shape of the transactional outbox. It lives in the service database so an event is committed
/// together with the state change that produced it.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; set; }

    public Guid? TenantId { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceParent { get; set; }

    public string? Source { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }
}
