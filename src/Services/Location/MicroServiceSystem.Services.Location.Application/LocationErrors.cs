using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Location.Application;
public static class LocationErrors
{
    public static readonly Error NotFound = Error.NotFound("location.not_found", "Country was not found.");
}
