using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Configuration;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults;

internal sealed class ServiceDefaultsOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<ServiceDefaultsOptions>
{
    public ValidateOptionsResult Validate(string? name, ServiceDefaultsOptions options)
    {
        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        if (options.EnableSwagger)
        {
            return ValidateOptionsResult.Fail(
                "ServiceDefaults:EnableSwagger must be false outside Development (secure-by-default).");
        }

        return ValidateOptionsResult.Success;
    }
}
