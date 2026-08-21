using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;
using MicroServiceSystem.Services.User.Application;
using MicroServiceSystem.Services.User.Infrastructure.BranchDatabases;

namespace MicroServiceSystem.Services.User.Infrastructure;

public static class UserInfrastructureExtensions
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "user", UserApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddIntegrationEventConsumers(
            configuration,
            UserApplicationExtensions.ApplicationAssembly,
            typeof(TenantDatabaseAccessChangedIntegrationEventHandler).Assembly);

        services.AddMemoryCache();
        services.Configure<IdentityCatalogOptions>(configuration.GetSection(IdentityCatalogOptions.SectionName));
        services.Configure<InternalServiceOptions>(configuration.GetSection(InternalServiceOptions.SectionName));
        services.AddHttpClient("user-identity-catalog");
        services.AddSingleton<ITenantConnectionStringProvider, IdentityCatalogTenantConnectionStringProvider>();
        services.AddHostedService<DevelopmentAdminProfileSeeder>();

        return services;
    }
}
