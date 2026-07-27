using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace Coordinator.Application.Registration;

/// <summary>
/// Mutable working set for the RegisterUser saga step runner.
/// </summary>
public sealed class RegisterUserSagaContext
{
    public RegisterUserSagaContext(
        RegisterUserSaga saga,
        StartRegisterUserSagaCommand command,
        string displayName,
        IRegisterUserSagaRepository sagas,
        IIdentityServiceClient identityClient,
        IUserServiceClient userClient,
        IIntegrationEventPublisher integrationEvents,
        ISagaCheckpoint checkpoint,
        IDateTimeProvider clock,
        TimeSpan leaseDuration)
    {
        Saga = saga;
        Command = command;
        DisplayName = displayName;
        Sagas = sagas;
        IdentityClient = identityClient;
        UserClient = userClient;
        IntegrationEvents = integrationEvents;
        Checkpoint = checkpoint;
        Clock = clock;
        LeaseDuration = leaseDuration;
    }

    public RegisterUserSaga Saga { get; }

    public StartRegisterUserSagaCommand Command { get; }

    public string DisplayName { get; }

    public IRegisterUserSagaRepository Sagas { get; }

    public IIdentityServiceClient IdentityClient { get; }

    public IUserServiceClient UserClient { get; }

    public IIntegrationEventPublisher IntegrationEvents { get; }

    public ISagaCheckpoint Checkpoint { get; }

    public IDateTimeProvider Clock { get; }

    public TimeSpan LeaseDuration { get; }

    public IdentityRegistrationResult? Identity { get; set; }

    public UserProfileResult? Profile { get; set; }

    /// <summary>
    /// Flushes saga progress and, while the saga is still running, pushes the lease out. Every checkpoint
    /// doubles as a heartbeat, so recovery only claims a saga once its owner really stopped making
    /// progress rather than because a remote call happened to be slow.
    /// </summary>
    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        if (!Saga.IsTerminal)
        {
            Saga.RenewLease(Clock.UtcNow, LeaseDuration);
        }

        Sagas.Update(Saga);
        await Checkpoint.CommitAsync(cancellationToken);
    }
}
