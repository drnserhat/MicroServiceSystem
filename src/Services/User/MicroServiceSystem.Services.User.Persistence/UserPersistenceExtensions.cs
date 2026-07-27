using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Persistence.Repositories;

namespace MicroServiceSystem.Services.User.Persistence;

public static class UserPersistenceExtensions
{
    public static IServiceCollection AddUserPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresPersistence<UserDbContext>(configuration, UserDbContext.DefaultSchema);
        services.AddEfMessagingStore<UserDbContext>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }
}
