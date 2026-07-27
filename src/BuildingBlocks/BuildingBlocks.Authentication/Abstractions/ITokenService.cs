namespace MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;

public interface ITokenService
{
    AccessToken CreateAccessToken(TokenSubject subject);

    RefreshTokenValue CreateRefreshToken();

    string ComputeRefreshTokenHash(string refreshToken);
}

public sealed record TokenSubject
{
    public required Guid UserId { get; init; }

    public required string UserName { get; init; }

    public string? Email { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? SessionId { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public IReadOnlyCollection<string> Permissions { get; init; } = [];

    public IReadOnlyDictionary<string, string> AdditionalClaims { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public sealed record RefreshTokenValue(string Value, string Hash, DateTimeOffset ExpiresAtUtc);
