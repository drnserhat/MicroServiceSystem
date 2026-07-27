using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Authentication;
using MicroServiceSystem.BuildingBlocks.Authorization.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authorization.Extensions;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission policies and makes authentication the default for every endpoint, so an
    /// endpoint becomes public only by explicitly allowing anonymous access.
    /// </summary>
    public static IServiceCollection AddFrameworkAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IPermissionProvider, ClaimsPermissionProvider>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(
                InternalApiKeyDefaults.PolicyName,
                policy => policy
                    .AddAuthenticationSchemes(InternalApiKeyDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim(FrameworkClaimTypes.TokenType, InternalApiKeyDefaults.TokenTypeValue));

        return services;
    }
}
