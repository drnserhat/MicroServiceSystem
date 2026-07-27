using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;

namespace MicroServiceSystem.Services.User.Application;

public static class UserApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(UserApplicationExtensions).Assembly;

    public static IServiceCollection AddUserApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);

        return services;
    }
}
