namespace MicroServiceSystem.Contracts.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }

    Guid? TenantId { get; }

    string? CorrelationId { get; }
}
