using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Extensions;
namespace MicroServiceSystem.Services.Audit.Application;
public static class AuditApplicationExtensions
{
    public static readonly Assembly ApplicationAssembly = typeof(AuditApplicationExtensions).Assembly;
    public static IServiceCollection AddAuditApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationBuildingBlock(configuration, ApplicationAssembly);
        return services;
    }
}
