using Microsoft.AspNetCore.Authorization;

namespace MicroServiceSystem.BuildingBlocks.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute(PermissionPolicy.ToPolicyName(permission))
{
    public string Permission { get; } = permission;
}
