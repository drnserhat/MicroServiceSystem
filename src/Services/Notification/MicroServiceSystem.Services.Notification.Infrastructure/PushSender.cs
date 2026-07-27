using Microsoft.Extensions.Logging;
using MicroServiceSystem.Services.Notification.Application.Abstractions;

namespace MicroServiceSystem.Services.Notification.Infrastructure;

/// <summary>
/// Development delivery adapter. Swap for SMTP/SMS/FCM providers in production without changing
/// application handlers.
/// </summary>
public sealed class PushSender(ILogger<PushSender> logger) : IPushSender
{
    public Task SendWelcomeAsync(
        Guid userId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Welcome notification queued for delivery to {Email} (user {UserId}, displayName {DisplayName})",
            email,
            userId,
            displayName);

        return Task.CompletedTask;
    }
}
