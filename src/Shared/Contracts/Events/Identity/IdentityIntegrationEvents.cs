using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.Contracts.Events.Identity;

[IntegrationEvent("identity.user_registered.v1")]
public sealed record UserRegisteredIntegrationEvent : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string UserName { get; init; }
}

[IntegrationEvent("identity.user_disabled.v1")]
public sealed record UserDisabledIntegrationEvent : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required string Reason { get; init; }
}
