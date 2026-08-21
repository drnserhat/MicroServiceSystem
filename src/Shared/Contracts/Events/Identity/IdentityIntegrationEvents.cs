using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.Contracts.Events.Identity;

[IntegrationEvent("identity.user_registered.v1")]
public sealed record UserRegisteredIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Identity account id. Profile rows are created only by the RegisterUser saga (HTTP), not by a
    /// choreography consumer of this event. Fan-out consumers (e.g. Audit) may project the registration
    /// fact without creating a User profile.
    /// </summary>
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

/// <summary>
/// Emitted when a tenant database binding changes access (disable / fail / reprovision).
/// Consumers must drop cached Npgsql data sources for the tenant (+ optional service key).
/// </summary>
[IntegrationEvent("identity.tenant_database_access_changed.v1")]
public sealed record TenantDatabaseAccessChangedIntegrationEvent : IntegrationEvent
{
    public required Guid BindingTenantId { get; init; }

    public required string ServiceKey { get; init; }

    public required string Status { get; init; }
}