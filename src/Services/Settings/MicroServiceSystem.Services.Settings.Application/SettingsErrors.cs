using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Settings.Application;
public static class SettingsErrors
{
    public static readonly Error NotFound = Error.NotFound("settings.not_found", "Setting was not found.");
}
