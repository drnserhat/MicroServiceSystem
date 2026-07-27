namespace MicroServiceSystem.Contracts.Abstractions;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public Guid? TenantId { get; init; }

    public string? CorrelationId { get; init; }
}
