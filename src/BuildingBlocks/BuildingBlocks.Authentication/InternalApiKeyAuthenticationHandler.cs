using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authentication;

public sealed class InternalApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<InternalServiceOptions> internalServiceOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        InternalServiceOptions settings = internalServiceOptions.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(settings.HeaderName, out Microsoft.Extensions.Primitives.StringValues headerValues)
            || string.IsNullOrWhiteSpace(headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string provided = headerValues.ToString();

        if (!FixedTimeEquals(settings.ApiKey, provided))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(FrameworkClaimTypes.TokenType, InternalApiKeyDefaults.TokenTypeValue),
                new Claim(ClaimTypes.Name, "internal-service"),
                new Claim(FrameworkClaimTypes.UserName, "internal-service")
            ],
            Scheme.Name);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
