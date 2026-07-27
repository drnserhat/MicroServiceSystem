using FluentValidation;
using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
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
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<StartRegisterUserSagaCommand, StartRegisterUserSagaResponse>
{
    public async Task<Result<StartRegisterUserSagaResponse>> Handle(
        StartRegisterUserSagaCommand command,
        CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        string displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? $"{command.FirstName.Trim()} {command.LastName.Trim()}"
            : command.DisplayName.Trim();

        RegisterUserSaga saga = RegisterUserSaga.Start(command.Email, command.UserName, displayName);
        saga.TenantId = command.TenantId;
        await sagas.AddAsync(saga, cancellationToken);

        IdentityRegistrationResult identityResult;
        try
        {
            identityResult = await identityClient.RegisterAsync(
                command.Email,
                command.UserName,
                command.Password,
                command.TenantId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            saga.MarkFailed(ex.Message);
            sagas.Update(saga);
            return CoordinatorErrors.IdentityRegistrationFailed;
        }

        saga.MarkIdentityRegistered(identityResult.UserId);
        sagas.Update(saga);

        UserProfileResult profileResult;
        try
        {
            profileResult = await userClient.CreateProfileAsync(
                identityResult.UserId,
                command.FirstName,
                command.LastName,
                displayName,
                command.TenantId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            saga.MarkCompensating(ex.Message);
            sagas.Update(saga);

            try
            {
                await identityClient.DisableAsync(
                    identityResult.UserId,
                    $"Compensating failed user profile creation: {ex.Message}",
                    command.TenantId,
                    cancellationToken);
            }
            catch
            {
                saga.MarkFailed("User profile creation failed and identity compensation failed.");
                sagas.Update(saga);
                return CoordinatorErrors.CompensationFailed;
            }

            saga.MarkFailed(ex.Message);
            sagas.Update(saga);
            return CoordinatorErrors.UserProfileCreationFailed;
        }

        saga.MarkUserProfileCreated(profileResult.Id);
        saga.MarkCompleted();
        sagas.Update(saga);

        await integrationEvents.PublishAsync(
            new WelcomeNotificationRequestedIntegrationEvent
            {
                UserId = identityResult.UserId,
                Email = identityResult.Email,
                DisplayName = displayName,
                TenantId = command.TenantId
            },
            cancellationToken);

        await integrationEvents.PublishAsync(
            new AuditEntryRequestedIntegrationEvent
            {
                Action = "user.registered",
                ResourceType = "User",
                ResourceId = identityResult.UserId.ToString(),
                ActorUserId = identityResult.UserId,
                Details = $"User {identityResult.Email} registered via coordinator saga {saga.Id}",
                TenantId = command.TenantId
            },
            cancellationToken);

        return new StartRegisterUserSagaResponse(saga.Id, identityResult.UserId, identityResult.Email, displayName);
    }
}
