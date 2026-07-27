using MicroServiceSystem.Services.User.Domain.Events;
using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.User.Domain.Aggregates;

public sealed class UserProfile : TenantAggregateRoot<Guid>
{
    private UserProfile()
    {
    }

    private UserProfile(Guid id, string firstName, string lastName, string displayName)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        IsActive = true;
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static UserProfile Create(Guid id, string firstName, string lastName, string? displayName = null)
    {
        Ensure.NotEmpty(id);
        Ensure.NotNullOrWhiteSpace(firstName);
        Ensure.NotNullOrWhiteSpace(lastName);
        Ensure.MaxLength(firstName, UserProfileConstraints.NameMaxLength);
        Ensure.MaxLength(lastName, UserProfileConstraints.NameMaxLength);

        string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{firstName.Trim()} {lastName.Trim()}"
            : displayName.Trim();

        Ensure.MaxLength(resolvedDisplayName, UserProfileConstraints.DisplayNameMaxLength);

        var profile = new UserProfile(
            id,
            firstName.Trim(),
            lastName.Trim(),
            resolvedDisplayName);

        profile.RaiseDomainEvent(new UserProfileCreatedDomainEvent(profile.Id, profile.DisplayName));

        return profile;
    }

    public void Update(string firstName, string lastName, string? displayName = null)
    {
        Ensure.NotNullOrWhiteSpace(firstName);
        Ensure.NotNullOrWhiteSpace(lastName);
        Ensure.MaxLength(firstName, UserProfileConstraints.NameMaxLength);
        Ensure.MaxLength(lastName, UserProfileConstraints.NameMaxLength);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{FirstName} {LastName}"
            : displayName.Trim();

        Ensure.MaxLength(DisplayName, UserProfileConstraints.DisplayNameMaxLength);

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, DisplayName));
    }

    public void Deactivate(string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RaiseDomainEvent(new UserProfileDeactivatedDomainEvent(Id, reason));
    }
}

public static class UserProfileConstraints
{
    public const int NameMaxLength = 128;

    public const int DisplayNameMaxLength = 256;
}
