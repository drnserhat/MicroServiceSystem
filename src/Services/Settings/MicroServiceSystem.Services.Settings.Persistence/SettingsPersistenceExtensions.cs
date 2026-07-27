using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Settings.Application.Abstractions;
using MicroServiceSystem.Services.Settings.Persistence.Repositories;
namespace MicroServiceSystem.Services.Settings.Persistence;
public static class SettingsPersistenceExtensions
{
    public static IServiceCollection AddSettingsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence<SettingsDbContext>(configuration, SettingsDbContext.DefaultSchema);
        services.AddEfMessagingStore<SettingsDbContext>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        return services;
    }
}
