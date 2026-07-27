using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Application;

public static class UserErrors
{
    public static readonly Error ProfileNotFound =
        Error.NotFound("user.profile_not_found", "User profile was not found.");

    public static readonly Error ProfileAlreadyExists =
        Error.Conflict("user.profile_already_exists", "A user profile with this id already exists.");

    public static readonly Error ProfileInactive =
        Error.Conflict("user.profile_inactive", "The user profile is already inactive.");
}
