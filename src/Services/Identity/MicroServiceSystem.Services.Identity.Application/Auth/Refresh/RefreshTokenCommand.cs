using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Tenants;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken, Guid TenantId) : ICommand<Login.LoginResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(
    IIdentityUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokenService,
    ICurrentTenant currentTenant,
    ITenantStore tenants,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshTokenCommand, Login.LoginResponse>
{
    public async Task<Result<Login.LoginResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        Result<TenantInfo> tenant =
            await TenantAccess.RequireActiveAsync(tenants, command.TenantId, cancellationToken);

        if (tenant.IsFailure)
        {
            return Result.Failure<Login.LoginResponse>(tenant.Error);
        }

        using IDisposable tenantScope = currentTenant.Change(command.TenantId, tenant.Value.Name);

        string tokenHash = tokenService.ComputeRefreshTokenHash(command.RefreshToken);
        RefreshToken? existing = await refreshTokens.FindByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
        {
            return IdentityErrors.RefreshTokenInvalid;
        }

        // A token that was already rotated is being presented again. The legitimate client moved on to
        // the replacement, so a replay means the token leaked; end the whole family rather than just
        // rejecting this one call, otherwise the thief keeps whatever token they stole.
        if (existing.RevokedAtUtc is not null)
        {
            foreach (RefreshToken active in await refreshTokens.ListActiveForUserAsync(
                existing.UserId,
                cancellationToken))
            {
                active.Revoke();
                refreshTokens.Update(active);
            }

            // The pipeline skips its commit for failed results, so the revocations are saved here.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return IdentityErrors.RefreshTokenReuseDetected;
        }

        if (!existing.IsActive)
        {
            return IdentityErrors.RefreshTokenInvalid;
        }

        IdentityUser? user = await users.GetByIdAsync(existing.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return IdentityErrors.UserDisabled;
        }

        RefreshTokenValue replacement = tokenService.CreateRefreshToken();
        existing.Revoke(replacement.Hash);
        refreshTokens.Update(existing);

        RefreshToken next = RefreshToken.Issue(user.Id, replacement.Hash, replacement.ExpiresAtUtc);
        next.TenantId = command.TenantId;
        await refreshTokens.AddAsync(next, cancellationToken);

        if (user.RoleIds.Count == 0)
        {
            Role memberRole = await AccessTokenFactory.GetOrCreateMemberRoleAsync(
                roles,
                command.TenantId,
                cancellationToken);
            user.AssignRole(memberRole.Id);
            users.Update(user);
        }

        AccessToken accessToken = await AccessTokenFactory.CreateForUserAsync(
            user,
            command.TenantId,
            roles,
            tokenService,
            cancellationToken);

        return new Login.LoginResponse(
            user.Id,
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            replacement.Value,
            replacement.ExpiresAtUtc);
    }
}
