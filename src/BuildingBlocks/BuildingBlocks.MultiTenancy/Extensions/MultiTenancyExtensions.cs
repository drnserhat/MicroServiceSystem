using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Resolvers;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Extensions;

public static class MultiTenancyExtensions
{
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MultiTenancyOptions>()
            .Bind(configuration.GetSection(MultiTenancyOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ICurrentTenant, CurrentTenant>();
        services.AddSingleton<ITenantResolver, ClaimTenantResolver>();
        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();

        return services;
    }

    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
