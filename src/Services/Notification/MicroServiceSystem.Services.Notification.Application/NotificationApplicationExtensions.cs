using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;
namespace MicroServiceSystem.Services.Notification.Application;
public static class NotificationApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(NotificationApplicationExtensions).Assembly;
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);
        return services;
    }
}
