using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Application;

public static class CoordinatorErrors
{
    public static readonly Error IdentityRegistrationFailed =
        Error.Failure("coordinator.identity_registration_failed", "Identity registration failed.");

    public static readonly Error UserProfileCreationFailed =
        Error.Failure("coordinator.user_profile_creation_failed", "User profile creation failed.");

    public static readonly Error CompensationFailed =
        Error.Failure("coordinator.compensation_failed", "Failed to compensate identity registration.");

    public static readonly Error SagaNotFound =
        Error.NotFound("coordinator.saga_not_found", "Registration saga was not found.");
}
