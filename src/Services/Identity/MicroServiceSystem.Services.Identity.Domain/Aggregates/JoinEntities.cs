using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.Services.Identity.Domain.Aggregates;

public sealed class UserRole : Entity<Guid>
{
    private UserRole()
    {
    }

    private UserRole(Guid id, Guid userId, Guid roleId)
        : base(id)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    internal static UserRole Create(Guid userId, Guid roleId) =>
        new(Guid.CreateVersion7(), userId, roleId);
}

public sealed class RolePermission : Entity<Guid>
{
    private RolePermission()
    {
    }

    private RolePermission(Guid id, Guid roleId, Guid permissionId)
        : base(id)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    internal static RolePermission Create(Guid roleId, Guid permissionId) =>
        new(Guid.CreateVersion7(), roleId, permissionId);
}
