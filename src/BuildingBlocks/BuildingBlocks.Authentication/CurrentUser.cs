using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authentication;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(FindClaim(FrameworkClaimTypes.UserId) ?? FindClaim(ClaimTypes.NameIdentifier), out Guid userId)
            ? userId
            : null;

    public string? UserName => FindClaim(FrameworkClaimTypes.UserName) ?? FindClaim(ClaimTypes.Name);

    public string? Email => FindClaim(FrameworkClaimTypes.Email) ?? FindClaim(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles => FindClaims(FrameworkClaimTypes.Role, ClaimTypes.Role);

    public IReadOnlyCollection<string> Permissions => FindClaims(FrameworkClaimTypes.Permission);

    public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.Ordinal);

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    private string? FindClaim(string claimType) => Principal?.FindFirst(claimType)?.Value;

    private IReadOnlyCollection<string> FindClaims(params string[] claimTypes)
    {
        ClaimsPrincipal? principal = Principal;

        if (principal is null)
        {
            return [];
        }

        return [.. principal.Claims
            .Where(claim => claimTypes.Contains(claim.Type, StringComparer.Ordinal))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)];
    }
}
