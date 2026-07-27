using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.Services.Identity.Application;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Auth.Login;
using MicroServiceSystem.Services.Identity.Application.Auth.Refresh;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
using NSubstitute;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Covers the replay branch of refresh rotation. Presenting a token that was already rotated means the
/// token leaked, so the whole family has to die rather than just the replayed token.
/// </summary>
public sealed class RefreshTokenReuseTests
{
    private const string PresentedToken = "presented-refresh-token";
    private const string PresentedHash = "presented-hash";

    [Fact]
    public async Task Replaying_a_rotated_token_revokes_every_remaining_token_for_the_user()
    {
        Guid userId = Guid.CreateVersion7();

        RefreshToken consumed = RefreshToken.Issue(userId, PresentedHash, DateTimeOffset.UtcNow.AddDays(7));
        consumed.Revoke("replacement-hash");

        RefreshToken stillActive = RefreshToken.Issue(
            userId,
            "replacement-hash",
            DateTimeOffset.UtcNow.AddDays(7));

        IRefreshTokenRepository refreshTokens = Substitute.For<IRefreshTokenRepository>();
        refreshTokens.FindByHashAsync(PresentedHash, Arg.Any<CancellationToken>()).Returns(consumed);
        refreshTokens.ListActiveForUserAsync(userId, Arg.Any<CancellationToken>()).Returns([stillActive]);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        RefreshTokenCommandHandler handler = CreateHandler(refreshTokens, unitOfWork);

        Result<LoginResponse> result = await handler.Handle(
            new RefreshTokenCommand(PresentedToken, Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(IdentityErrors.RefreshTokenReuseDetected.Code);

        stillActive.RevokedAtUtc.ShouldNotBeNull();
        refreshTokens.Received(1).Update(stillActive);

        // The pipeline skips its commit for failed results, so the handler has to persist the revocations
        // itself; without this the family would stay usable.
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_token_is_rejected_without_touching_any_other_session()
    {
        IRefreshTokenRepository refreshTokens = Substitute.For<IRefreshTokenRepository>();
        refreshTokens.FindByHashAsync(PresentedHash, Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        RefreshTokenCommandHandler handler = CreateHandler(refreshTokens, unitOfWork);

        Result<LoginResponse> result = await handler.Handle(
            new RefreshTokenCommand(PresentedToken, Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);

        await refreshTokens.DidNotReceive().ListActiveForUserAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_expired_token_is_rejected_as_invalid_rather_than_as_a_replay()
    {
        Guid userId = Guid.CreateVersion7();
        RefreshToken expired = RefreshToken.Issue(userId, PresentedHash, DateTimeOffset.UtcNow.AddDays(-1));

        IRefreshTokenRepository refreshTokens = Substitute.For<IRefreshTokenRepository>();
        refreshTokens.FindByHashAsync(PresentedHash, Arg.Any<CancellationToken>()).Returns(expired);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        RefreshTokenCommandHandler handler = CreateHandler(refreshTokens, unitOfWork);

        Result<LoginResponse> result = await handler.Handle(
            new RefreshTokenCommand(PresentedToken, Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
        await refreshTokens.DidNotReceive().ListActiveForUserAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static RefreshTokenCommandHandler CreateHandler(
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork)
    {
        ITokenService tokenService = Substitute.For<ITokenService>();
        tokenService.ComputeRefreshTokenHash(PresentedToken).Returns(PresentedHash);

        return new RefreshTokenCommandHandler(
            Substitute.For<IIdentityUserRepository>(),
            Substitute.For<IRoleRepository>(),
            refreshTokens,
            tokenService,
            new CurrentTenant(),
            CreateActiveTenantStore(),
            unitOfWork);
    }

    private static ITenantStore CreateActiveTenantStore()
    {
        ITenantStore tenants = Substitute.For<ITenantStore>();
        tenants.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<TenantInfo?>(
                new TenantInfo(call.ArgAt<Guid>(0), "Demo") { IsActive = true }));
        return tenants;
    }
}
