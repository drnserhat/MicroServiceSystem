using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Contracts.Events.User;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.Services.User.Application.IntegrationEvents;

/// <summary>
/// Keeps the user profile in sync when Identity disables an account (e.g. saga compensation).
/// </summary>
public sealed class UserDisabledIntegrationEventHandler(
    IUserProfileRepository profiles,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : IIntegrationEventHandler<UserDisabledIntegrationEvent>
{
    public async Task HandleAsync(UserDisabledIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable tenantScope = currentTenant.Change(tenantId);

        UserProfile? profile = await profiles.GetByIdAsync(integrationEvent.UserId, cancellationToken);

        if (profile is null || !profile.IsActive)
        {
            return;
        }

        profile.Deactivate(integrationEvent.Reason);
        profiles.Update(profile);

        await integrationEvents.PublishAsync(
            new UserProfileDeactivatedIntegrationEvent
            {
                UserId = profile.Id,
                Reason = integrationEvent.Reason,
                TenantId = tenantId
            },
            cancellationToken);
    }
}
