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

    public static readonly Error RefreshTokenReuseDetected =
        Error.Unauthorized(
            "identity.refresh_token_reuse_detected",
            "The refresh token was already used. All sessions for this account have been signed out.");

    public static readonly Error TenantNotFound =
        Error.NotFound("identity.tenant_not_found", "Tenant was not found.");

    public static readonly Error TenantInactive =
        Error.Forbidden("identity.tenant_inactive", "Tenant is inactive.");

    public static readonly Error TenantSlugTaken =
        Error.Conflict("identity.tenant_slug_taken", "A tenant with this slug already exists.");

    public static readonly Error TenantAlreadyExists =
        Error.Conflict("identity.tenant_already_exists", "A tenant with this id already exists.");

    public static readonly Error RoleNotFound =
        Error.NotFound("identity.role_not_found", "Role was not found in this tenant.");

    public static readonly Error LastAdminProtected =
        Error.Conflict(
            "identity.last_admin_protected",
            "Cannot remove the Admin role from the last active administrator in this tenant.");
}
