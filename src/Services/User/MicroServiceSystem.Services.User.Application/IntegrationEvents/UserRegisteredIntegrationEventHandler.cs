using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Contracts.Events.User;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.Services.User.Application.IntegrationEvents;

public sealed class UserRegisteredIntegrationEventHandler(
    IUserProfileRepository profiles,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public async Task HandleAsync(UserRegisteredIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable tenantScope = currentTenant.Change(tenantId);

        if (await profiles.ExistsAsync(integrationEvent.UserId, cancellationToken))
        {
            return;
        }

        (string firstName, string lastName) = SplitUserName(integrationEvent.UserName);

        UserProfile profile = UserProfile.Create(
            integrationEvent.UserId,
            firstName,
            lastName);

        profile.TenantId = tenantId;

        await profiles.AddAsync(profile, cancellationToken);

        await integrationEvents.PublishAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserId = profile.Id,
                DisplayName = profile.DisplayName,
                TenantId = tenantId
            },
            cancellationToken);
    }

    private static (string FirstName, string LastName) SplitUserName(string userName)
    {
        string trimmed = userName.Trim();
        string[] parts = trimmed.Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return (trimmed, trimmed);
        }

        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
