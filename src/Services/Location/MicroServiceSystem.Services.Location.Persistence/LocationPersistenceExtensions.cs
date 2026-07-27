using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Persistence.Repositories;
namespace MicroServiceSystem.Services.Location.Persistence;
public static class LocationPersistenceExtensions
{
    public static IServiceCollection AddLocationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence<LocationDbContext>(configuration, LocationDbContext.DefaultSchema);
        services.AddEfMessagingStore<LocationDbContext>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        return services;
    }
}
