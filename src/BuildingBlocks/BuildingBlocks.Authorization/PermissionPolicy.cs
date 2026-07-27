namespace MicroServiceSystem.BuildingBlocks.Authorization;

/// <summary>
/// Translates a permission into its policy name and back, so permission strings never appear as
/// duplicated literals across controllers and endpoints.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "permission:";

    public static string ToPolicyName(string permission) => $"{Prefix}{permission}";

    public static bool TryGetPermission(string policyName, out string permission)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            permission = policyName[Prefix.Length..];
            return true;
        }

        permission = string.Empty;
        return false;
    }
}
