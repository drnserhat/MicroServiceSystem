using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.User;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Application.Profiles.Create;

public sealed record CreateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid TenantId) : ICommand<UserProfileResponse>;

public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive);

public sealed class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
{
    public CreateUserProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(UserProfileConstraints.NameMaxLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(UserProfileConstraints.NameMaxLength);
        RuleFor(command => command.DisplayName).MaximumLength(UserProfileConstraints.DisplayNameMaxLength)
            .When(command => command.DisplayName is not null);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class CreateUserProfileCommandHandler(
    IUserProfileRepository profiles,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<CreateUserProfileCommand, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        CreateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        UserProfile? existing = await profiles.GetByIdAsync(command.UserId, cancellationToken);

        if (existing is not null)
        {
            // Idempotent for RegisterUser race: event consumer or saga may create first.
            existing.Update(command.FirstName, command.LastName, command.DisplayName);
            profiles.Update(existing);

            return new UserProfileResponse(
                existing.Id,
                existing.FirstName,
                existing.LastName,
                existing.DisplayName,
                existing.IsActive);
        }

        UserProfile profile = UserProfile.Create(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.DisplayName);

        profile.TenantId = command.TenantId;

        await profiles.AddAsync(profile, cancellationToken);

        await integrationEvents.PublishAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserId = profile.Id,
                DisplayName = profile.DisplayName,
                TenantId = command.TenantId
            },
            cancellationToken);

        return new UserProfileResponse(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.IsActive);
    }
}
