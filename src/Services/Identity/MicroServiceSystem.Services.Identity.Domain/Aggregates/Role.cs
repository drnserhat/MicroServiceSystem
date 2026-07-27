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

    public static Role Create(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Ensure.MaxLength(name, 128);

        return new Role(Guid.CreateVersion7(), name.Trim());
    }

    public void GrantPermission(string permissionCode)
    {
        Ensure.NotNullOrWhiteSpace(permissionCode);

        if (!_permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
        {
            _permissions.Add(permissionCode);
        }
    }
}
