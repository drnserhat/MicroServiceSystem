using MicroServiceSystem.Services.Identity.Domain.Rules;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

public sealed class Role : TenantAggregateRoot<Guid>
{
    private readonly List<string> _permissions = [];

    private Role()
    {
    }

    private Role(Guid id, string name)
        : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public IReadOnlyCollection<string> Permissions => _permissions;

    public bool IsBuiltIn =>
        string.Equals(NormalizedName, FrameworkPermissions.AdminRoleName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(NormalizedName, FrameworkPermissions.MemberRoleName, StringComparison.OrdinalIgnoreCase);

    public static Role Create(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, 128);

        return new Role(Guid.CreateVersion7(), name.Trim());
    }

    /// <summary>
    /// Creates a tenant-defined role. Reserved Admin/Member names are rejected here; seed paths use
    /// <see cref="Create"/>.
    /// </summary>
    public static Role CreateCustom(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, 128);

        string trimmed = name.Trim();
        CheckRule(new BuiltInRoleNameMustNotBeUsedForCustomRoleRule(FrameworkPermissions.IsBuiltInRoleName(trimmed)));

        return new Role(Guid.CreateVersion7(), trimmed);
    }

    public void GrantPermission(string permissionCode)
    {
        Ensure.NotNullOrWhiteSpace(permissionCode);

        if (!_permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
        {
            _permissions.Add(permissionCode);
        }
    }

    public void Rename(string name)
    {
        CheckRule(new BuiltInRoleMustNotBeMutatedRule(IsBuiltIn));
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, 128);

        string trimmed = name.Trim();
        CheckRule(new BuiltInRoleNameMustNotBeUsedForCustomRoleRule(FrameworkPermissions.IsBuiltInRoleName(trimmed)));

        Name = trimmed;
        NormalizedName = trimmed.ToUpperInvariant();
    }

    public void ReplacePermissions(IEnumerable<string> permissionCodes)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);
        CheckRule(new BuiltInRoleMustNotBeMutatedRule(IsBuiltIn));

        _permissions.Clear();

        foreach (string code in permissionCodes)
        {
            GrantPermission(code);
        }
    }

    public void EnsureCanDelete() => CheckRule(new BuiltInRoleMustNotBeMutatedRule(IsBuiltIn));
}
