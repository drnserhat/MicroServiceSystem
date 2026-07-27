using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.SharedKernel.Results;

public static class FrameworkErrors
{
    public static Error NotFound(string resource, object identifier) =>
        Error.NotFound(FrameworkErrorCodes.NotFound, $"{resource} '{identifier}' was not found.");

    public static Error Conflict(string description) => Error.Conflict(FrameworkErrorCodes.Conflict, description);

    public static Error Validation(IReadOnlyDictionary<string, string[]> failures) =>
        Error.Validation(FrameworkErrorCodes.Validation, "One or more validation failures occurred.", failures);

    public static Error Unauthorized(string description = "Authentication is required.") =>
        Error.Unauthorized(FrameworkErrorCodes.Unauthorized, description);

    public static Error Forbidden(string description = "Access to the requested resource is denied.") =>
        Error.Forbidden(FrameworkErrorCodes.Forbidden, description);

    public static Error Concurrency(string resource) =>
        Error.Conflict(FrameworkErrorCodes.Concurrency, $"{resource} was modified by another operation.");

    public static Error TenantMissing() =>
        Error.Failure(FrameworkErrorCodes.TenantMissing, "The request could not be associated with a tenant.");

    public static Error DependencyUnavailable(string dependency) =>
        Error.Unavailable(FrameworkErrorCodes.DependencyUnavailable, $"Dependency '{dependency}' is unavailable.");

    public static Error Unexpected(string description = "An unexpected error occurred.") =>
        Error.Failure(FrameworkErrorCodes.Unexpected, description);
}
