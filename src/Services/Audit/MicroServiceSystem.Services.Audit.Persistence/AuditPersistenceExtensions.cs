using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Persistence.Repositories;
namespace MicroServiceSystem.Services.Audit.Persistence;
public static class AuditPersistenceExtensions
{
    public static IServiceCollection AddAuditPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence<AuditDbContext>(configuration, AuditDbContext.DefaultSchema);
        services.AddEfMessagingStore<AuditDbContext>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        return services;
    }
}
