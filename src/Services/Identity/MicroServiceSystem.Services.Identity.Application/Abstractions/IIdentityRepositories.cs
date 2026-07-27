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
}

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
}
