using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.Contracts.Events.Notification;

[IntegrationEvent("notification.welcome_requested.v1")]
public sealed record WelcomeNotificationRequestedIntegrationEvent : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }
}
