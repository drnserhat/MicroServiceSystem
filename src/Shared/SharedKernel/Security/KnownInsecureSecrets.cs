namespace MicroServiceSystem.SharedKernel.Security;

/// <summary>
/// Well-known placeholder secrets shipped for local Development only.
/// Production startup must reject these values.
/// </summary>
public static class KnownInsecureSecrets
{
    public const string DevelopmentJwtSigningKey = "0123456789abcdef0123456789abcdef01234567";

    public const string DevelopmentInternalApiKey = "dev-internal-api-key-change-me";

    public static bool IsDevelopmentJwtSigningKey(string? signingKey) =>
        string.Equals(signingKey, DevelopmentJwtSigningKey, StringComparison.Ordinal);

    public static bool IsDevelopmentInternalApiKey(string? apiKey) =>
        string.Equals(apiKey, DevelopmentInternalApiKey, StringComparison.Ordinal);
}
