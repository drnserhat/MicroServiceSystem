using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

internal sealed class MultiTenancyOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<MultiTenancyOptions>
{
    public ValidateOptionsResult Validate(string? name, MultiTenancyOptions options)
    {
        if (environment.IsDevelopment() || !options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        if (!options.RequireTenant)
        {
            failures.Add(
                "MultiTenancy:RequireTenant must be true outside Development (secure-by-default).");
        }

        if (options.TrustTenantHeader)
        {
            failures.Add(
                "MultiTenancy:TrustTenantHeader must be false outside Development. " +
                "Only the gateway may inject a validated tenant header in Production.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
