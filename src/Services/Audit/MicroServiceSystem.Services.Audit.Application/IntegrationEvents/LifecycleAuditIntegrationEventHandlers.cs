using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Contracts.Events.User;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.Services.Audit.Application.IntegrationEvents;

public sealed class UserRegisteredAuditIntegrationEventHandler(
    IAuditEntryRepository entries,
    ICurrentTenant tenant) : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public async Task HandleAsync(UserRegisteredIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable scope = tenant.Change(tenantId);

        AuditEntry entry = AuditEntry.Create(
            "identity.user.registered",
            "identity_user",
            integrationEvent.UserId.ToString(),
            integrationEvent.UserId,
            $"email={integrationEvent.Email}; userName={integrationEvent.UserName}");

        entry.TenantId = tenantId;
        await entries.AddAsync(entry, cancellationToken);
    }
}

public sealed class UserProfileCreatedAuditIntegrationEventHandler(
    IAuditEntryRepository entries,
    ICurrentTenant tenant) : IIntegrationEventHandler<UserProfileCreatedIntegrationEvent>
{
    public async Task HandleAsync(
        UserProfileCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable scope = tenant.Change(tenantId);

        AuditEntry entry = AuditEntry.Create(
            "user.profile.created",
            "user_profile",
            integrationEvent.UserId.ToString(),
            integrationEvent.UserId,
            integrationEvent.DisplayName);

        entry.TenantId = tenantId;
        await entries.AddAsync(entry, cancellationToken);
    }
}

public sealed class UserDisabledAuditIntegrationEventHandler(
    IAuditEntryRepository entries,
    ICurrentTenant tenant) : IIntegrationEventHandler<UserDisabledIntegrationEvent>
{
    public async Task HandleAsync(UserDisabledIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable scope = tenant.Change(tenantId);

        AuditEntry entry = AuditEntry.Create(
            "identity.user.disabled",
            "identity_user",
            integrationEvent.UserId.ToString(),
            integrationEvent.UserId,
            integrationEvent.Reason);

        entry.TenantId = tenantId;
        await entries.AddAsync(entry, cancellationToken);
    }
}

public sealed class UserProfileDeactivatedAuditIntegrationEventHandler(
    IAuditEntryRepository entries,
    ICurrentTenant tenant) : IIntegrationEventHandler<UserProfileDeactivatedIntegrationEvent>
{
    public async Task HandleAsync(
        UserProfileDeactivatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable scope = tenant.Change(tenantId);

        AuditEntry entry = AuditEntry.Create(
            "user.profile.deactivated",
            "user_profile",
            integrationEvent.UserId.ToString(),
            integrationEvent.UserId,
            integrationEvent.Reason);

        entry.TenantId = tenantId;
        await entries.AddAsync(entry, cancellationToken);
    }
}
