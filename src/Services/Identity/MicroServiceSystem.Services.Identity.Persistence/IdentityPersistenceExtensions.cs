using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Persistence.Repositories;
using MicroServiceSystem.Services.Identity.Persistence.Tenancy;

namespace MicroServiceSystem.Services.Identity.Persistence;

public static class IdentityPersistenceExtensions
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresPersistence<IdentityDbContext>(configuration, IdentityDbContext.DefaultSchema);
        services.AddEfMessagingStore<IdentityDbContext>();

        services.AddScoped<IIdentityUserRepository, IdentityUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantStore, EfTenantStore>();

        return services;
    }
}
