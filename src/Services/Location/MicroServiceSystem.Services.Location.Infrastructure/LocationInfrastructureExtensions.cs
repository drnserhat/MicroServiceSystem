using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Location.Application;

namespace MicroServiceSystem.Services.Location.Infrastructure;

public static class LocationInfrastructureExtensions
{
    public static IServiceCollection AddLocationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "location", LocationApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddHostedService<DevelopmentCountryCatalogSeeder>();

        return services;
    }
}
