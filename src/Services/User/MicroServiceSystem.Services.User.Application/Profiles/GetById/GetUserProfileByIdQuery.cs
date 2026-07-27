using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Application.Profiles.Create;
using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Application.Profiles.GetById;

public sealed record GetUserProfileByIdQuery(Guid UserId) : IQuery<UserProfileResponse>;

public sealed class GetUserProfileByIdQueryValidator : AbstractValidator<GetUserProfileByIdQuery>
{
    public GetUserProfileByIdQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}

public sealed class GetUserProfileByIdQueryHandler(IUserProfileRepository profiles)
    : IQueryHandler<GetUserProfileByIdQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetUserProfileByIdQuery query,
        CancellationToken cancellationToken)
    {
        UserProfile? profile = await profiles.GetByIdAsync(query.UserId, cancellationToken);

        if (profile is null)
        {
            return UserErrors.ProfileNotFound;
        }

        return new UserProfileResponse(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.IsActive);
    }
}
