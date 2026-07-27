using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Resolvers;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Extensions;

public static class MultiTenancyExtensions
{
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration) =>
        AddMultiTenancy(services, configuration, environment: null);

    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        services.AddOptions<MultiTenancyOptions>()
            .Bind(configuration.GetSection(MultiTenancyOptions.SectionName))
            .ValidateOnStart();

        if (environment is not null)
        {
            services.AddSingleton<IValidateOptions<MultiTenancyOptions>>(
                new MultiTenancyOptionsValidator(environment));
        }
        else
        {
            services.AddSingleton<IValidateOptions<MultiTenancyOptions>, MultiTenancyOptionsValidator>();
        }

        services.AddSingleton<ICurrentTenant, CurrentTenant>();
        services.AddSingleton<ITenantResolver, ClaimTenantResolver>();
        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();

        return services;
    }

    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
