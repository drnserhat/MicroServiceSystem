using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;

namespace MicroServiceSystem.Services.MsfService.Application;

public static class MsfServiceApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(MsfServiceApplicationExtensions).Assembly;

    public static IServiceCollection AddMsfServiceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);

        return services;
    }
}
