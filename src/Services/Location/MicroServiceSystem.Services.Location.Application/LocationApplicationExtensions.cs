using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;
namespace MicroServiceSystem.Services.Location.Application;
public static class LocationApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(LocationApplicationExtensions).Assembly;
    public static IServiceCollection AddLocationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);
        return services;
    }
}
