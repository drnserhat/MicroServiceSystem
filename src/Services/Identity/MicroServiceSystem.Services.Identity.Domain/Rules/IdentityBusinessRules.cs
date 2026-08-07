using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Rules;

public sealed class EmailMustBeValidRule(string email) : IBusinessRule
{
    public string Code => "identity.email_invalid";

    public string Message => "Email address is not valid.";

    public bool IsBroken() =>
        string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal);
}

public sealed class UserMustBeActiveRule(bool isActive) : IBusinessRule
{
    public string Code => "identity.user_inactive";

    public string Message => "The user account is inactive.";

    public bool IsBroken() => !isActive;
}

public sealed class UserMustNotBeLockedOutRule(bool isLockedOut, DateTimeOffset? lockoutEndUtc, DateTimeOffset utcNow)
    : IBusinessRule
{
    public string Code => "identity.user_locked_out";

    public string Message => "The user account is locked out.";

    public bool IsBroken() => isLockedOut && lockoutEndUtc is { } end && end > utcNow;
}

public sealed class RoleNameMustBeUniqueWithinTenantRule(bool exists) : IBusinessRule
{
    public string Code => "identity.role_name_exists";

    public string Message => "A role with the same name already exists for this tenant.";

    public bool IsBroken() => exists;
}

public sealed class BuiltInRoleMustNotBeMutatedRule(bool isBuiltIn) : IBusinessRule
{
    public string Code => "identity.built_in_role_protected";

    public string Message => "Built-in Admin and Member roles cannot be renamed, changed, or deleted.";

    public bool IsBroken() => isBuiltIn;
}

public sealed class BuiltInRoleNameMustNotBeUsedForCustomRoleRule(bool isReservedName) : IBusinessRule
{
    public string Code => "identity.role_name_reserved";

    public string Message => "Admin and Member are reserved role names.";

    public bool IsBroken() => isReservedName;
}
