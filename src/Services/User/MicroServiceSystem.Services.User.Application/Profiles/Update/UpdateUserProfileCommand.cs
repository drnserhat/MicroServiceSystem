using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Application.Profiles.Create;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Application.Profiles.Update;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    uint ExpectedVersion) : ICommand<UserProfileResponse>;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(UserProfileConstraints.NameMaxLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(UserProfileConstraints.NameMaxLength);
        RuleFor(command => command.DisplayName).MaximumLength(UserProfileConstraints.DisplayNameMaxLength)
            .When(command => command.DisplayName is not null);
    }
}

public sealed class UpdateUserProfileCommandHandler(
    IUserProfileRepository profiles,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserProfileCommand, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        UserProfile? profile = await profiles.GetByIdAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            return UserErrors.ProfileNotFound;
        }

        if (!profile.IsActive)
        {
            return UserErrors.ProfileInactive;
        }

        profiles.SetExpectedConcurrencyVersion(profile, command.ExpectedVersion);
        profile.Update(command.FirstName, command.LastName, command.DisplayName);
        profiles.Update(profile);

        // Persist before reading xmin so the response Version / ETag matches the stored row.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserProfileResponse(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.IsActive,
            profiles.GetConcurrencyVersion(profile));
    }
}
