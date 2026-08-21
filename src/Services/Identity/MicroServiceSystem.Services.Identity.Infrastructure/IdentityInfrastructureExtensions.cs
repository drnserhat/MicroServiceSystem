using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Identity.Application;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Infrastructure.BranchDatabases;

namespace MicroServiceSystem.Services.Identity.Infrastructure;

public static class IdentityInfrastructureExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "identity", IdentityApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<InternalServiceOptions>(configuration.GetSection(InternalServiceOptions.SectionName));
        services.AddHttpClient("identity-user-provision");
        services.AddScoped<ITenantDatabaseProvisioner, TenantDatabaseProvisioner>();
        services.AddHostedService<DevelopmentTenantSeeder>();
        services.AddHostedService<DevelopmentBranchDatabaseSeeder>();
        services.AddHostedService<DevelopmentAdminSeeder>();

        return services;
    }
}
