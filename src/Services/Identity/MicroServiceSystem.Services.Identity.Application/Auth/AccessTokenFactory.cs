using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Identity.Application.Auth;

public static class AccessTokenFactory
{
    public const string MemberRoleName = FrameworkPermissions.MemberRoleName;

    public static async Task<Role> GetOrCreateMemberRoleAsync(
        IRoleRepository roles,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        Role? existing = await roles.FindByNameAsync(MemberRoleName, cancellationToken);

        if (existing is not null)
        {
            SyncPermissions(existing, FrameworkPermissions.MemberDefaults);
            roles.Update(existing);
            return existing;
        }

        Role role = Role.Create(MemberRoleName);
        role.TenantId = tenantId;

        foreach (string permission in FrameworkPermissions.MemberDefaults)
        {
            role.GrantPermission(permission);
        }

        await roles.AddAsync(role, cancellationToken);

        return role;
    }

    public static async Task<Role> GetOrCreateAdminRoleAsync(
        IRoleRepository roles,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        Role? existing = await roles.FindByNameAsync(FrameworkPermissions.AdminRoleName, cancellationToken);

        if (existing is not null)
        {
            SyncPermissions(existing, FrameworkPermissions.AdminDefaults);
            roles.Update(existing);
            return existing;
        }

        Role role = Role.Create(FrameworkPermissions.AdminRoleName);
        role.TenantId = tenantId;

        foreach (string permission in FrameworkPermissions.AdminDefaults)
        {
            role.GrantPermission(permission);
        }

        await roles.AddAsync(role, cancellationToken);

        return role;
    }

    private static void SyncPermissions(Role role, IReadOnlyList<string> expected)
    {
        foreach (string permission in expected)
        {
            role.GrantPermission(permission);
        }
    }

    public static async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> ResolveClaimsAsync(
        IdentityUser user,
        IRoleRepository roles,
        CancellationToken cancellationToken)
    {
        if (user.RoleIds.Count == 0)
        {
            return ([], []);
        }

        IReadOnlyList<Role> assigned = await roles.ListByIdsAsync(user.RoleIds, cancellationToken);

        string[] roleNames = assigned.Select(role => role.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string[] permissions = assigned
            .SelectMany(role => role.Permissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return (roleNames, permissions);
    }

    public static async Task<AccessToken> CreateForUserAsync(
        IdentityUser user,
        Guid tenantId,
        IRoleRepository roles,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<string> roleNames, IReadOnlyList<string> permissions) =
            await ResolveClaimsAsync(user, roles, cancellationToken);

        return tokenService.CreateAccessToken(new TokenSubject
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            TenantId = tenantId,
            Roles = roleNames,
            Permissions = permissions
        });
    }
}
