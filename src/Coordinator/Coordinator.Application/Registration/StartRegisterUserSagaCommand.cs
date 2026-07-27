using FluentValidation;
using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.Contracts.Events.Audit;
using MicroServiceSystem.Contracts.Events.Notification;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Application.Registration;

public sealed record StartRegisterUserSagaCommand(
    string Email,
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid TenantId) : ICommand<StartRegisterUserSagaResponse>;

public sealed record StartRegisterUserSagaResponse(Guid SagaId, Guid UserId, string Email, string DisplayName);

public sealed class StartRegisterUserSagaCommandValidator : AbstractValidator<StartRegisterUserSagaCommand>
{
    public StartRegisterUserSagaCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.UserName).NotEmpty().MinimumLength(3).MaximumLength(128);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.DisplayName).MaximumLength(256).When(command => command.DisplayName is not null);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class StartRegisterUserSagaCommandHandler(
    IRegisterUserSagaRepository sagas,
    IIdentityServiceClient identityClient,
    IUserServiceClient userClient,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents,
    ISagaCheckpoint checkpoint,
    IDateTimeProvider clock,
    IOptions<SagaOptions> sagaOptions) : ICommandHandler<StartRegisterUserSagaCommand, StartRegisterUserSagaResponse>
{
    public async Task<Result<StartRegisterUserSagaResponse>> Handle(
        StartRegisterUserSagaCommand command,
        CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid callerTenantId || callerTenantId != command.TenantId)
        {
            return Result.Failure<StartRegisterUserSagaResponse>(CoordinatorErrors.TenantScopeMismatch);
        }

        // Catalog check before any durable write: an invented TenantId must never become a saga row.
        TenantCatalogResult? tenant = await identityClient.GetTenantAsync(command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure<StartRegisterUserSagaResponse>(CoordinatorErrors.TenantNotFound);
        }

        if (!tenant.IsActive)
        {
            return Result.Failure<StartRegisterUserSagaResponse>(CoordinatorErrors.TenantInactive);
        }

        using IDisposable tenantScope = currentTenant.Change(command.TenantId, tenant.Name);

        string displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? $"{command.FirstName.Trim()} {command.LastName.Trim()}"
            : command.DisplayName.Trim();

        var leaseDuration = TimeSpan.FromSeconds(Math.Max(30, sagaOptions.Value.LeaseSeconds));

        RegisterUserSaga saga = RegisterUserSaga.Start(command.Email, command.UserName, displayName);
        saga.TenantId = command.TenantId;

        // Claim the saga up front so the recovery worker treats it as owned rather than abandoned.
        saga.AcquireLease(SagaOwner.Current, clock.UtcNow, leaseDuration);

        await sagas.AddAsync(saga, cancellationToken);
        // Durable: persist Started before any remote side effect.
        await checkpoint.CommitAsync(cancellationToken);

        var sagaContext = new RegisterUserSagaContext(
            saga,
            command,
            displayName,
            sagas,
            identityClient,
            userClient,
            integrationEvents,
            checkpoint,
            clock,
            leaseDuration);

        // Deliberately not the request token. Once the first remote call is in flight, abandoning the saga
        // because the client hung up would strand a half-created user; the lease lets recovery finish it
        // if this process dies instead.
        Result sagaResult = await SagaRunner.RunAsync(
            [
                new RegisterIdentityStep(),
                new CreateUserProfileStep()
            ],
            sagaContext,
            CancellationToken.None);

        // From here on the remote work already happened, so these writes also ignore the request token.
        if (sagaResult.IsFailure)
        {
            // Compensating is a deliberate non-terminal state meaning "the undo still has to happen".
            // Failing it here would hide the saga from the recovery worker and leak the identity it created.
            if (!saga.IsTerminal && saga.State != RegisterUserSagaState.Compensating)
            {
                saga.MarkFailed(sagaResult.Error.Description);
                await sagaContext.PersistAsync(CancellationToken.None);
            }

            return Result.Failure<StartRegisterUserSagaResponse>(sagaResult.Error);
        }

        if (sagaContext.Identity is null || sagaContext.Profile is null)
        {
            saga.MarkFailed("Saga completed without identity or profile results.");
            await sagaContext.PersistAsync(CancellationToken.None);
            return Result.Failure<StartRegisterUserSagaResponse>(CoordinatorErrors.UserProfileCreationFailed);
        }

        saga.MarkCompleted();
        sagas.Update(saga);

        await integrationEvents.PublishAsync(
            new WelcomeNotificationRequestedIntegrationEvent
            {
                UserId = sagaContext.Identity.UserId,
                Email = sagaContext.Identity.Email,
                DisplayName = displayName,
                TenantId = command.TenantId
            },
            CancellationToken.None);

        await integrationEvents.PublishAsync(
            new AuditEntryRequestedIntegrationEvent
            {
                Action = "user.registered",
                ResourceType = "User",
                ResourceId = sagaContext.Identity.UserId.ToString(),
                ActorUserId = sagaContext.Identity.UserId,
                Details = $"User {sagaContext.Identity.Email} registered via coordinator saga {saga.Id}",
                TenantId = command.TenantId
            },
            CancellationToken.None);

        // Terminal success + outbox rows committed by UnitOfWorkBehavior.
        return new StartRegisterUserSagaResponse(
            saga.Id,
            sagaContext.Identity.UserId,
            sagaContext.Identity.Email,
            displayName);
    }
}
