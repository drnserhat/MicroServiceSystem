using Coordinator.Application;
using Coordinator.Application.Abstractions;
using Coordinator.Application.Registration;
using Coordinator.Domain.Aggregates;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.SharedKernel.Results;
using NSubstitute;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Covers the durability contract of the registration saga: the identity id is reserved before the remote
/// call, the saga is leased while it runs, and a failure always undoes the identity it may have created.
/// </summary>
public sealed class RegisterUserSagaTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();

    [Fact]
    public async Task Identity_id_is_reserved_and_checkpointed_before_the_remote_call()
    {
        var harness = new SagaHarness();

        await harness.RunAsync();

        // The id the remote call was made with must already have been persisted on the saga, otherwise a
        // crash during the call leaves a user that no recovery pass can find.
        harness.IdentityIdOnSagaAtCall.ShouldNotBeNull();
        harness.IdentityIdOnSagaAtCall.ShouldBe(harness.IdentityIdPassedToRemote);

        // Two commits: the Started row, then the reservation.
        harness.CommitsBeforeRemoteCall.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task A_successful_registration_completes_the_saga_and_releases_the_lease()
    {
        var harness = new SagaHarness();

        Result<StartRegisterUserSagaResponse> result = await harness.RunAsync();

        result.IsSuccess.ShouldBeTrue();
        harness.Saga!.State.ShouldBe(RegisterUserSagaState.Completed);
        harness.Saga.LockedUntilUtc.ShouldBeNull();
        harness.Saga.LockedBy.ShouldBeNull();
    }

    [Fact]
    public async Task The_saga_is_leased_while_it_runs_so_recovery_leaves_it_alone()
    {
        var harness = new SagaHarness();
        DateTimeOffset? leaseDuringRun = null;

        harness.IdentityClient
            .RegisterAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                leaseDuringRun = harness.Saga!.LockedUntilUtc;
                return Task.FromResult(
                    new IdentityRegistrationResult(call.ArgAt<Guid>(0), "user@test.local", "tester"));
            });

        await harness.RunAsync();

        leaseDuringRun.ShouldNotBeNull();
        leaseDuringRun!.Value.ShouldBeGreaterThan(harness.Clock.UtcNow);
    }

    [Fact]
    public async Task A_failed_identity_registration_disables_the_reserved_identity()
    {
        var harness = new SagaHarness();
        harness.IdentityClient
            .RegisterAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<IdentityRegistrationResult>>(_ => throw new HttpRequestException("identity down"));

        Result<StartRegisterUserSagaResponse> result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();

        // The call may still have created the user before the response was lost, so the undo must target
        // the reserved id rather than being skipped.
        await harness.IdentityClient.Received(1).DisableAsync(
            harness.ReservedIdentityId,
            Arg.Any<string>(),
            TenantId,
            Arg.Any<CancellationToken>());

        harness.Saga!.State.ShouldBe(RegisterUserSagaState.Failed);
    }

    [Fact]
    public async Task A_failed_profile_creation_compensates_the_identity()
    {
        var harness = new SagaHarness();
        harness.UserClient
            .CreateProfileAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<UserProfileResult>>(_ => throw new HttpRequestException("user service down"));

        Result<StartRegisterUserSagaResponse> result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        await harness.IdentityClient.Received(1).DisableAsync(
            harness.ReservedIdentityId,
            Arg.Any<string>(),
            TenantId,
            Arg.Any<CancellationToken>());

        harness.Saga!.State.ShouldBe(RegisterUserSagaState.Failed);
    }

    [Fact]
    public async Task Compensation_that_keeps_failing_leaves_the_saga_recoverable()
    {
        var harness = new SagaHarness();
        harness.UserClient
            .CreateProfileAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<UserProfileResult>>(_ => throw new HttpRequestException("user service down"));

        harness.IdentityClient
            .DisableAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new HttpRequestException("identity down too"));

        await harness.RunAsync();

        // Non-terminal on purpose: the recovery worker has to be able to retry the undo.
        harness.Saga!.State.ShouldBe(RegisterUserSagaState.Compensating);
        harness.Saga.IsTerminal.ShouldBeFalse();
    }

    [Fact]
    public async Task An_unknown_tenant_is_rejected_before_any_saga_row_is_written()
    {
        var harness = new SagaHarness();
        harness.IdentityClient
            .GetTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantCatalogResult?>(null));

        Result<StartRegisterUserSagaResponse> result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(CoordinatorErrors.TenantNotFound.Code);
        await harness.Sagas.DidNotReceive().AddAsync(
            Arg.Any<RegisterUserSaga>(),
            TestContext.Current.CancellationToken);
        await harness.IdentityClient.DidNotReceive().RegisterAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task An_inactive_tenant_is_rejected_before_any_saga_row_is_written()
    {
        var harness = new SagaHarness();
        harness.IdentityClient
            .GetTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<TenantCatalogResult?>(
                new TenantCatalogResult(call.ArgAt<Guid>(0), "Frozen", "frozen", IsActive: false)));

        Result<StartRegisterUserSagaResponse> result = await harness.RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(CoordinatorErrors.TenantInactive.Code);
        await harness.Sagas.DidNotReceive().AddAsync(
            Arg.Any<RegisterUserSaga>(),
            TestContext.Current.CancellationToken);
    }

    private sealed class SagaHarness
    {
        private readonly StartRegisterUserSagaCommandHandler _handler;
        private int _commits;

        public SagaHarness()
        {
            Sagas = Substitute.For<IRegisterUserSagaRepository>();
            Sagas.AddAsync(Arg.Do<RegisterUserSaga>(saga => Saga = saga), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            IdentityClient = Substitute.For<IIdentityServiceClient>();
            IdentityClient
                .GetTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult<TenantCatalogResult?>(
                    new TenantCatalogResult(call.ArgAt<Guid>(0), "Demo", "demo", IsActive: true)));

            IdentityClient
                .RegisterAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    CommitsBeforeRemoteCall = _commits;
                    IdentityIdOnSagaAtCall = Saga?.IdentityUserId;
                    IdentityIdPassedToRemote = call.ArgAt<Guid>(0);

                    return Task.FromResult(
                        new IdentityRegistrationResult(call.ArgAt<Guid>(0), "user@test.local", "tester"));
                });

            UserClient = Substitute.For<IUserServiceClient>();
            UserClient
                .CreateProfileAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new UserProfileResult(Guid.CreateVersion7(), "Ada", "Lovelace", "Ada Lovelace", true)));

            Checkpoint = Substitute.For<ISagaCheckpoint>();
            Checkpoint.When(checkpoint => checkpoint.CommitAsync(Arg.Any<CancellationToken>()))
                .Do(_ => _commits++);

            Clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };

            var ambientTenant = new CurrentTenant();
            _ = ambientTenant.Change(TenantId, "Demo");

            _handler = new StartRegisterUserSagaCommandHandler(
                Sagas,
                IdentityClient,
                UserClient,
                ambientTenant,
                Substitute.For<IIntegrationEventPublisher>(),
                Checkpoint,
                Clock,
                Options.Create(new SagaOptions { LeaseSeconds = 120 }));
        }

        public IRegisterUserSagaRepository Sagas { get; }

        public IIdentityServiceClient IdentityClient { get; }

        public IUserServiceClient UserClient { get; }

        public ISagaCheckpoint Checkpoint { get; }

        public MutableDateTimeProvider Clock { get; }

        public RegisterUserSaga? Saga { get; private set; }

        public Guid ReservedIdentityId => Saga?.IdentityUserId ?? Guid.Empty;

        public int CommitsBeforeRemoteCall { get; private set; }

        public Guid? IdentityIdOnSagaAtCall { get; private set; }

        public Guid? IdentityIdPassedToRemote { get; private set; }

        public Task<Result<StartRegisterUserSagaResponse>> RunAsync() =>
            _handler.Handle(
                new StartRegisterUserSagaCommand(
                    "user@test.local",
                    "tester",
                    "Sup3rSecret!",
                    "Ada",
                    "Lovelace",
                    null,
                    TenantId),
                TestContext.Current.CancellationToken);
    }
}
