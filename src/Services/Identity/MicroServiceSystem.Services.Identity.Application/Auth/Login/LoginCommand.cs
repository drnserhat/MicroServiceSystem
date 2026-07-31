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

namespace MicroServiceSystem.Services.Identity.Application.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    Guid TenantId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    Guid UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IIdentityUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentTenant currentTenant,
    ITenantStore tenants,
    IDateTimeProvider clock) : ICommandHandler<LoginCommand, LoginResponse>
{
    private const int MaxFailedAccessAttempts = 5;

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        Result<TenantInfo> tenant =
            await TenantAccess.RequireActiveAsync(tenants, command.TenantId, cancellationToken);

        if (tenant.IsFailure)
        {
            return Result.Failure<LoginResponse>(tenant.Error);
        }

        using IDisposable tenantScope = currentTenant.Change(command.TenantId, tenant.Value.Name);

        IdentityUser? user = await users.FindByEmailAsync(command.Email, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.InvalidCredentials;
        }

        if (!user.IsActive)
        {
            return IdentityErrors.UserDisabled;
        }

        if (user.IsCurrentlyLockedOut(clock.UtcNow))
        {
            return IdentityErrors.UserLockedOut;
        }

        PasswordVerificationResult verification = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (verification is PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin(MaxFailedAccessAttempts, TimeSpan.FromMinutes(15));
            users.Update(user);

            return IdentityErrors.InvalidCredentials;
        }

        if (verification is PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ReplacePasswordHash(passwordHasher.Hash(command.Password));
        }

        user.RecordSuccessfulLogin();

        // Keep role permission catalogs aligned with FrameworkPermissions defaults on each login.
        await AccessTokenFactory.GetOrCreateMemberRoleAsync(roles, command.TenantId, cancellationToken);
        await AccessTokenFactory.GetOrCreateAdminRoleAsync(roles, command.TenantId, cancellationToken);

        if (user.RoleIds.Count == 0)
        {
            Role memberRole = await AccessTokenFactory.GetOrCreateMemberRoleAsync(
                roles,
                command.TenantId,
                cancellationToken);
            user.AssignRole(memberRole.Id);
        }

        users.Update(user);

        AccessToken accessToken = await AccessTokenFactory.CreateForUserAsync(
            user,
            command.TenantId,
            roles,
            tokenService,
            cancellationToken);

        RefreshTokenValue refresh = tokenService.CreateRefreshToken();
        RefreshToken refreshToken = RefreshToken.Issue(user.Id, refresh.Hash, refresh.ExpiresAtUtc);
        refreshToken.TenantId = command.TenantId;

        await refreshTokens.AddAsync(refreshToken, cancellationToken);

        return new LoginResponse(
            user.Id,
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refresh.Value,
            refresh.ExpiresAtUtc);
    }
}
