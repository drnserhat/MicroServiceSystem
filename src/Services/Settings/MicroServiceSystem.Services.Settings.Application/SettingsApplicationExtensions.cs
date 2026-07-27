using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;
namespace MicroServiceSystem.Services.Settings.Application;
public static class SettingsApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(SettingsApplicationExtensions).Assembly;
    public static IServiceCollection AddSettingsApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);
        return services;
    }
}
