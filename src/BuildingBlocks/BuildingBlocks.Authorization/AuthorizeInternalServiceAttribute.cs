using Microsoft.AspNetCore.Authorization;
using MicroServiceSystem.BuildingBlocks.Authentication;

namespace MicroServiceSystem.BuildingBlocks.Authorization;

/// <summary>
/// Restricts an endpoint to callers presenting a valid internal service API key.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeInternalServiceAttribute : AuthorizeAttribute
{
    public AuthorizeInternalServiceAttribute()
    {
        AuthenticationSchemes = InternalApiKeyDefaults.AuthenticationScheme;
        Policy = InternalApiKeyDefaults.PolicyName;
    }
}
