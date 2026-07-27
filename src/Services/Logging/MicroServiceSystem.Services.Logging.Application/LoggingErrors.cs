using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Logging.Application;

public static class LoggingErrors
{
    public static readonly Error NotFound =
        Error.NotFound("logging.not_found", "System log entry was not found.");

    public static readonly Error InvalidTimeRange =
        Error.Validation(
            "logging.invalid_time_range",
            "fromUtc must be less than or equal to toUtc.");
}
