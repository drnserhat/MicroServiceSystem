using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Settings.Application;

public static class SettingsErrors
{
    public static readonly Error NotFound =
        Error.NotFound("settings.not_found", "Setting was not found.");

    public static readonly Error ConcurrencyTokenRequired =
        Error.Validation(
            "settings.concurrency_token_required",
            "If-Match with the current setting version is required to update an existing key.");
}
