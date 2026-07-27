using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application;

public static class IdentityErrors
{
    public static readonly Error EmailAlreadyRegistered =
        Error.Conflict("identity.email_already_registered", "A user with this email already exists.");

    public static readonly Error UserNameAlreadyTaken =
        Error.Conflict("identity.username_already_taken", "A user with this username already exists.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Invalid email or password.");

    public static readonly Error UserLockedOut =
        Error.Forbidden("identity.user_locked_out", "The account is temporarily locked.");

    public static readonly Error UserDisabled =
        Error.Forbidden("identity.user_disabled", "The account is disabled.");

    public static readonly Error UserNotFound =
        Error.NotFound("identity.user_not_found", "Identity user was not found.");

    public static readonly Error RefreshTokenInvalid =
        Error.Unauthorized("identity.refresh_token_invalid", "Refresh token is invalid or expired.");
}
