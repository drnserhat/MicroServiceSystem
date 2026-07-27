using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Notification.Application.Abstractions;
using MicroServiceSystem.Services.Notification.Persistence.Repositories;
namespace MicroServiceSystem.Services.Notification.Persistence;
public static class NotificationPersistenceExtensions
{
    public static IServiceCollection AddNotificationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence<NotificationDbContext>(configuration, NotificationDbContext.DefaultSchema);
        services.AddEfMessagingStore<NotificationDbContext>();
        services.AddScoped<INotificationMessageRepository, NotificationMessageRepository>();
        return services;
    }
}
