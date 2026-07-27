using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.File.Application.Abstractions;
using MicroServiceSystem.Services.File.Persistence.Repositories;
namespace MicroServiceSystem.Services.File.Persistence;
public static class FilePersistenceExtensions
{
    public static IServiceCollection AddFilePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence<FileDbContext>(configuration, FileDbContext.DefaultSchema);
        services.AddEfMessagingStore<FileDbContext>();
        services.AddScoped<IFileAssetRepository, FileAssetRepository>();
        return services;
    }
}
