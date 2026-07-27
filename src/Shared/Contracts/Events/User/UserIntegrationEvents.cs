using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.Contracts.Events.User;

[IntegrationEvent("user.profile_created.v1")]
public sealed record UserProfileCreatedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Raised when a profile row is created (RegisterUser saga HTTP). Audit (and other projections)
    /// should react here — do not also create the profile from <c>UserRegistered</c>.
    /// </summary>
    public required Guid UserId { get; init; }

    public required string DisplayName { get; init; }
}

[IntegrationEvent("user.profile_deactivated.v1")]
public sealed record UserProfileDeactivatedIntegrationEvent : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required string Reason { get; init; }
}
