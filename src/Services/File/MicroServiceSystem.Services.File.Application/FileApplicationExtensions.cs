using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;
namespace MicroServiceSystem.Services.File.Application;
public static class FileApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(FileApplicationExtensions).Assembly;
    public static IServiceCollection AddFileApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);
        return services;
    }
}
