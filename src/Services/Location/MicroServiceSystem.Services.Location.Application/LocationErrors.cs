using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Location.Application;

public static class LocationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("location.not_found", "Country was not found.");

    public static readonly Error CodeAlreadyExists =
        Error.Conflict("location.code_already_exists", "A country with this code already exists.");

    public static readonly Error ConcurrencyTokenRequired =
        Error.Validation(
            "location.concurrency_token_required",
            "If-Match with the current country version is required to update or delete.");
}
