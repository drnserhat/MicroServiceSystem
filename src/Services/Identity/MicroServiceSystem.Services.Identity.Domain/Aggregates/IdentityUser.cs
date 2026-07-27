using MicroServiceSystem.Services.Identity.Domain.Events;
using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

public sealed class IdentityUser : TenantAggregateRoot<Guid>
{
    private readonly List<Guid> _roleIds = [];

    private IdentityUser()
    {
    }

    private IdentityUser(Guid id, string email, string userName, string passwordHash)
        : base(id)
    {
        Email = email;
        UserName = userName;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;

    public string UserName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool EmailConfirmed { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsLockedOut { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public int AccessFailedCount { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Guid> RoleIds => _roleIds;

    public static IdentityUser Register(string email, string userName, string passwordHash)
    {
        Ensure.NotNullOrWhiteSpace(email);
        Ensure.NotNullOrWhiteSpace(userName);
        Ensure.NotNullOrWhiteSpace(passwordHash);
        Ensure.MaxLength(email, IdentityUserConstraints.EmailMaxLength);
        Ensure.MaxLength(userName, IdentityUserConstraints.UserNameMaxLength);

        var user = new IdentityUser(
            Guid.CreateVersion7(),
            email.Trim().ToLowerInvariant(),
            userName.Trim(),
            passwordHash);

        user.RaiseDomainEvent(new IdentityUserRegisteredDomainEvent(user.Id, user.Email, user.UserName));

        return user;
    }

    public void Disable(string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RaiseDomainEvent(new IdentityUserDisabledDomainEvent(Id, reason));
    }

    public void RecordFailedLogin(int maxFailedAccessAttempts, TimeSpan lockoutDuration)
    {
        AccessFailedCount++;

        if (AccessFailedCount >= maxFailedAccessAttempts)
        {
            IsLockedOut = true;
            LockoutEndUtc = DateTimeOffset.UtcNow.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void RecordSuccessfulLogin()
    {
        AccessFailedCount = 0;
        IsLockedOut = false;
        LockoutEndUtc = null;
    }

    public bool IsCurrentlyLockedOut(DateTimeOffset utcNow) =>
        IsLockedOut && LockoutEndUtc is { } end && end > utcNow;

    public void ReplacePasswordHash(string passwordHash)
    {
        Ensure.NotNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void AssignRole(Guid roleId)
    {
        if (!_roleIds.Contains(roleId))
        {
            _roleIds.Add(roleId);
        }
    }
}

public static class IdentityUserConstraints
{
    public const int EmailMaxLength = 256;

    public const int UserNameMaxLength = 128;

    public const int PasswordHashMaxLength = 512;
}
