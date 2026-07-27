using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence.Repositories;

public sealed class IdentityUserRepository(IdentityDbContext context)
    : EfRepository<IdentityUser, Guid>(context), IIdentityUserRepository
{
    public Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(user => user.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public Task<IdentityUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(user => user.UserName == userName.Trim(), cancellationToken);
}

public sealed class RefreshTokenRepository(IdentityDbContext context)
    : EfRepository<RefreshToken, Guid>(context), IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await Set
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
}

public sealed class RoleRepository(IdentityDbContext context)
    : EfRepository<Role, Guid>(context), IRoleRepository
{
    public Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(role => role.NormalizedName == name.Trim().ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Role>> ListByIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        Guid[] ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        return await Set.Where(role => ids.Contains(role.Id)).ToListAsync(cancellationToken);
    }
}

public sealed class TenantRepository(IdentityDbContext context)
    : EfRepository<Tenant, Guid>(context), ITenantRepository
{
    public Task<Tenant?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(
            tenant => tenant.Slug == Tenant.NormalizeSlug(slug),
            cancellationToken);
}
