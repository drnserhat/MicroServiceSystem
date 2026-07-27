using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Identity.Application;

namespace MicroServiceSystem.Services.Identity.Infrastructure;

public static class IdentityInfrastructureExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "identity", IdentityApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();

        return services;
    }
}
