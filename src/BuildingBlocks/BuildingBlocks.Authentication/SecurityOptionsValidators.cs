using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.SharedKernel.Security;

namespace MicroServiceSystem.BuildingBlocks.Authentication;

internal sealed class JwtOptionsValidator(IHostEnvironment environment) : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        if (!options.RequireHttpsMetadata)
        {
            failures.Add(
                "Authentication:Jwt:RequireHttpsMetadata must be true outside Development.");
        }

        if (KnownInsecureSecrets.IsDevelopmentJwtSigningKey(options.SigningKey))
        {
            failures.Add(
                "Authentication:Jwt:SigningKey uses the Development placeholder and is not allowed outside Development.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

internal sealed class InternalServiceOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<InternalServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, InternalServiceOptions options)
    {
        if (environment.IsDevelopment() || !options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:InternalService:ApiKey is required when Enabled outside Development.");
        }

        if (KnownInsecureSecrets.IsDevelopmentInternalApiKey(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:InternalService:ApiKey uses the Development placeholder and is not allowed outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
