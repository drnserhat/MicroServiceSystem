using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.Services.Identity.Application.Abstractions;

public interface IIdentityUserRepository : IRepository<IdentityUser, Guid>
{
    Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IdentityUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every token for the user that has not been revoked yet, so a detected replay can end the
    /// whole family rather than just the replayed token.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
}

public interface ITenantRepository : IRepository<Tenant, Guid>
{
    Task<Tenant?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
