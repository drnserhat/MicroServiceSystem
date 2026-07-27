using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Authentication;
using MicroServiceSystem.BuildingBlocks.Authorization.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authorization.Extensions;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission policies. When <paramref name="requireAuthenticatedByDefault"/> is true,
    /// every endpoint requires authentication unless it explicitly allows anonymous access.
    /// Gateways should keep this true and mark public YARP routes with
    /// <c>AuthorizationPolicy = Anonymous</c> instead of disabling the fallback policy.
    /// </summary>
    public static IServiceCollection AddFrameworkAuthorization(
        this IServiceCollection services,
        bool requireAuthenticatedByDefault = true)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IPermissionProvider, ClaimsPermissionProvider>();

        AuthorizationBuilder authorizationBuilder = services.AddAuthorizationBuilder();

        if (requireAuthenticatedByDefault)
        {
            authorizationBuilder.SetFallbackPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        authorizationBuilder.AddPolicy(
            InternalApiKeyDefaults.PolicyName,
            policy => policy
                .AddAuthenticationSchemes(InternalApiKeyDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(FrameworkClaimTypes.TokenType, InternalApiKeyDefaults.TokenTypeValue));

        return services;
    }
}
