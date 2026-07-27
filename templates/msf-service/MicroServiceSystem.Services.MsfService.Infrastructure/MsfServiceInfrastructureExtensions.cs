using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.MsfService.Application;

namespace MicroServiceSystem.Services.MsfService.Infrastructure;

public static class MsfServiceInfrastructureExtensions
{
    public static IServiceCollection AddMsfServiceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMqMessaging(configuration, "msfservice", MsfServiceApplicationExtensions.ApplicationAssembly);
#if (publishesEvents)
        services.AddOutboxProcessing(configuration);
#endif
#if (consumesEvents)
        services.AddIntegrationEventConsumers(configuration, MsfServiceApplicationExtensions.ApplicationAssembly);
#endif

        return services;
    }
}
