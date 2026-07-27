using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider) : ITokenService
{
    private const int RefreshTokenSizeInBytes = 64;

    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken CreateAccessToken(TokenSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        JwtOptions jwtOptions = options.Value;
        DateTimeOffset issuedAt = dateTimeProvider.UtcNow;
        DateTimeOffset expiresAt = issuedAt.AddMinutes(jwtOptions.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(BuildClaims(subject)),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    public RefreshTokenValue CreateRefreshToken()
    {
        string value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenSizeInBytes));
        DateTimeOffset expiresAt = dateTimeProvider.UtcNow.AddDays(options.Value.RefreshTokenLifetimeDays);

        return new RefreshTokenValue(value, ComputeRefreshTokenHash(value), expiresAt);
    }

    public string ComputeRefreshTokenHash(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToBase64String(hash);
    }

    private static List<Claim> BuildClaims(TokenSubject subject)
    {
        List<Claim> claims =
        [
            new(FrameworkClaimTypes.UserId, subject.UserId.ToString()),
            new(FrameworkClaimTypes.UserName, subject.UserName)
        ];

        if (!string.IsNullOrWhiteSpace(subject.Email))
        {
            claims.Add(new Claim(FrameworkClaimTypes.Email, subject.Email));
        }

        if (subject.TenantId is { } tenantId)
        {
            claims.Add(new Claim(FrameworkClaimTypes.TenantId, tenantId.ToString()));
        }

        if (subject.SessionId is { } sessionId)
        {
            claims.Add(new Claim(FrameworkClaimTypes.SessionId, sessionId.ToString()));
        }

        claims.AddRange(subject.Roles.Select(role => new Claim(FrameworkClaimTypes.Role, role)));
        claims.AddRange(subject.Permissions.Select(permission => new Claim(FrameworkClaimTypes.Permission, permission)));
        claims.AddRange(subject.AdditionalClaims.Select(claim => new Claim(claim.Key, claim.Value)));

        return claims;
    }
}
