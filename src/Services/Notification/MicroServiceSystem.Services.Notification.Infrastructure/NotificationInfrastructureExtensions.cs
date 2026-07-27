using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.Services.Notification.Application;

namespace MicroServiceSystem.Services.Notification.Infrastructure;
public static class NotificationInfrastructureExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "notification", NotificationApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddIntegrationEventConsumers(configuration, NotificationApplicationExtensions.ApplicationAssembly);
        services.AddSingleton<MicroServiceSystem.Services.Notification.Application.Abstractions.IPushSender, PushSender>();
        
        return services;
    }
}
