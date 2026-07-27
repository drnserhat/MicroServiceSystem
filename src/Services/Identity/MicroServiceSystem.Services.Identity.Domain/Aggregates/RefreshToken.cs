using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

public sealed class RefreshToken : TenantAggregateRoot<Guid>
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        Ensure.NotEmpty(userId);
        Ensure.NotNullOrWhiteSpace(tokenHash);

        return new RefreshToken(Guid.CreateVersion7(), userId, tokenHash, expiresAtUtc);
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
