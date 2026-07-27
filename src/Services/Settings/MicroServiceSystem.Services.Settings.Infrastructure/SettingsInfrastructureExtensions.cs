using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Settings.Application;

namespace MicroServiceSystem.Services.Settings.Infrastructure;
public static class SettingsInfrastructureExtensions
{
    public static IServiceCollection AddSettingsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "settings", SettingsApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        
        return services;
    }
}
