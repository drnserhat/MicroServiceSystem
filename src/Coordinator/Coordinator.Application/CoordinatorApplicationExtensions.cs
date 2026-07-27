using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;

namespace Coordinator.Application;

public static class CoordinatorApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(CoordinatorApplicationExtensions).Assembly;

    public static IServiceCollection AddCoordinatorApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);

        return services;
    }
}
