using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.File.Application;
using MicroServiceSystem.BuildingBlocks.Storage.Extensions;

namespace MicroServiceSystem.Services.File.Infrastructure;
public static class FileInfrastructureExtensions
{
    public static IServiceCollection AddFileInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "file", FileApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddFrameworkStorage(configuration);
        
        return services;
    }
}
