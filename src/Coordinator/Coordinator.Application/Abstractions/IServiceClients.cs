using Coordinator.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace Coordinator.Application.Abstractions;

public interface IRegisterUserSagaRepository : IRepository<RegisterUserSaga, Guid>
{
    /// <summary>
    /// Returns non-terminal sagas that no live owner holds any more, that is whose lease is absent or
    /// already expired at <paramref name="utcNow"/>.
    /// </summary>
    Task<IReadOnlyList<RegisterUserSaga>> ListAbandonedAsync(
        DateTimeOffset utcNow,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Ops read: newest-first saga page, optional exact state filter.</summary>
    Task<IReadOnlyList<RegisterUserSaga>> ListForOpsAsync(
        string? state,
        int take,
        CancellationToken cancellationToken = default);
}

public interface IIdentityServiceClient
{
    /// <summary>
    /// Looks up a tenant in Identity's catalog. Returns <see langword="null"/> when the id is unknown.
    /// </summary>
    Task<TenantCatalogResult?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="userId"/> is reserved by the saga before the call, which makes the registration
    /// safe to retry and gives compensation a target even if the response is lost.
    /// </summary>
    Task<IdentityRegistrationResult> RegisterAsync(
        Guid userId,
        string email,
        string userName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task DisableAsync(Guid userId, string reason, Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantCatalogResult(Guid Id, string Name, string Slug, bool IsActive);

public sealed record IdentityRegistrationResult(Guid UserId, string Email, string UserName);

public interface IUserServiceClient
{
    Task<UserProfileResult> CreateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? displayName,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record UserProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive);
