using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using Coordinator.Application.Abstractions;
using Coordinator.Persistence.Repositories;

namespace Coordinator.Persistence;

public static class CoordinatorPersistenceExtensions
{
    public static IServiceCollection AddCoordinatorPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresPersistence<CoordinatorDbContext>(configuration, CoordinatorDbContext.DefaultSchema);
        services.AddEfMessagingStore<CoordinatorDbContext>();

        services.AddScoped<IRegisterUserSagaRepository, RegisterUserSagaRepository>();

        return services;
    }
}
