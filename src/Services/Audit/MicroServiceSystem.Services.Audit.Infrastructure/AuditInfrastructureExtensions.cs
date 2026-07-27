using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Audit.Application;

namespace MicroServiceSystem.Services.Audit.Infrastructure;
public static class AuditInfrastructureExtensions
{
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "audit", AuditApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddIntegrationEventConsumers(configuration, AuditApplicationExtensions.ApplicationAssembly);
        
        return services;
    }
}
