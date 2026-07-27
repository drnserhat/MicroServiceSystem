using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.Contracts.Events.Audit;

[IntegrationEvent("audit.entry_requested.v1")]
public sealed record AuditEntryRequestedIntegrationEvent : IntegrationEvent
{
    public required string Action { get; init; }

    public required string ResourceType { get; init; }

    public required string ResourceId { get; init; }

    public Guid? ActorUserId { get; init; }

    public string? Details { get; init; }
}
