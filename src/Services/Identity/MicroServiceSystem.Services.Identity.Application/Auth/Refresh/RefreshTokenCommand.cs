using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
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
    ICurrentTenant currentTenant) : ICommandHandler<RefreshTokenCommand, Login.LoginResponse>
{
    public async Task<Result<Login.LoginResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        string tokenHash = tokenService.ComputeRefreshTokenHash(command.RefreshToken);
        RefreshToken? existing = await refreshTokens.FindByHashAsync(tokenHash, cancellationToken);

        if (existing is null || !existing.IsActive)
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
