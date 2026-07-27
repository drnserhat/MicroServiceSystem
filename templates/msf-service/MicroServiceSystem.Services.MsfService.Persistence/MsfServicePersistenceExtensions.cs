using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;

namespace MicroServiceSystem.Services.MsfService.Persistence;

public static class MsfServicePersistenceExtensions
{
    public static IServiceCollection AddMsfServicePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresPersistence<MsfServiceDbContext>(configuration, MsfServiceDbContext.DefaultSchema);

        return services;
    }
}
