using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.User;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Application.Profiles.Deactivate;

public sealed record DeactivateUserProfileCommand(Guid UserId, string Reason, Guid TenantId) : ICommand;

public sealed class DeactivateUserProfileCommandValidator : AbstractValidator<DeactivateUserProfileCommand>
{
    public DeactivateUserProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(512);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class DeactivateUserProfileCommandHandler(
    IUserProfileRepository profiles,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<DeactivateUserProfileCommand>
{
    public async Task<Result> Handle(DeactivateUserProfileCommand command, CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        UserProfile? profile = await profiles.GetByIdAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(UserErrors.ProfileNotFound);
        }

        if (!profile.IsActive)
        {
            return Result.Failure(UserErrors.ProfileInactive);
        }

        profile.Deactivate(command.Reason);
        profiles.Update(profile);

        await integrationEvents.PublishAsync(
            new UserProfileDeactivatedIntegrationEvent
            {
                UserId = profile.Id,
                Reason = command.Reason,
                TenantId = command.TenantId
            },
            cancellationToken);

        return Result.Success();
    }
}
