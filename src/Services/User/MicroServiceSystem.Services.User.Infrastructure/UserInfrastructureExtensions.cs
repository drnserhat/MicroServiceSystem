using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.User.Application;

namespace MicroServiceSystem.Services.User.Infrastructure;

public static class UserInfrastructureExtensions
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "user", UserApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddIntegrationEventConsumers(configuration, UserApplicationExtensions.ApplicationAssembly);

        return services;
    }
}
