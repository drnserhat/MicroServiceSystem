using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Authentication;

/// <summary>
/// PBKDF2 with HMAC-SHA512. The iteration count is persisted with the hash so raising it later keeps
/// existing credentials verifiable while signalling a rehash on the next successful sign in.
/// </summary>
public sealed class Pbkdf2PasswordHasher(IOptions<PasswordPolicyOptions> options) : IPasswordHasher
{
    private const string Version = "v1";
    private const int SaltSizeInBytes = 16;
    private const int HashSizeInBytes = 32;
    private const char SegmentSeparator = '.';

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        int iterations = options.Value.HashIterations;
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, HashSizeInBytes);

        return string.Join(
            SegmentSeparator,
            Version,
            iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return PasswordVerificationResult.Failed;
        }

        string[] segments = passwordHash.Split(SegmentSeparator);

        if (segments.Length != 4 || !string.Equals(segments[0], Version, StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }

        if (!int.TryParse(segments[1], CultureInfo.InvariantCulture, out int iterations) || iterations <= 0)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(segments[2]);
            expectedHash = Convert.FromBase64String(segments[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            expectedHash.Length);

        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        return iterations < options.Value.HashIterations
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }
}
