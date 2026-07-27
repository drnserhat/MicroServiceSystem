using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;

namespace MicroServiceSystem.Services.Identity.Application;

public static class IdentityApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(IdentityApplicationExtensions).Assembly;

    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);

        return services;
    }
}
