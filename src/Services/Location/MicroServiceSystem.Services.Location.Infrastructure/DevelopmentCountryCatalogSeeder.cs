using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.Services.Location.Infrastructure;

/// <summary>
/// Seeds ISO 3166-1 alpha-2 countries for the local demo tenant. Idempotent by code;
/// does not overwrite admin renames. Production must provision catalogs explicitly
/// (or enable via configuration later) — no live external country API.
/// </summary>
public sealed class DevelopmentCountryCatalogSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DevelopmentCountryCatalogSeeder> logger) : IHostedService
{
    /// <summary>Same demo tenant as Identity <c>KnownTenants.DevelopmentDemo</c> / admin default.</summary>
    public static readonly Guid DevelopmentDemoTenantId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ICountryRepository countries = scope.ServiceProvider.GetRequiredService<ICountryRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();

            using IDisposable tenantScope = currentTenant.Change(DevelopmentDemoTenantId, "Development Demo");

            int inserted = 0;
            foreach ((string code, string name) in IsoCountryCatalog.Entries)
            {
                if (await countries.FindByCodeAsync(code, cancellationToken) is not null)
                {
                    continue;
                }

                Country country = Country.Create(code, name);
                country.TenantId = DevelopmentDemoTenantId;
                await countries.AddAsync(country, cancellationToken);
                inserted++;
            }

            if (inserted > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Seeded {Inserted} ISO countries for tenant {TenantId} (catalog size {CatalogSize})",
                    inserted,
                    DevelopmentDemoTenantId,
                    IsoCountryCatalog.Entries.Count);
            }
            else
            {
                logger.LogDebug(
                    "Country catalog already present for tenant {TenantId}",
                    DevelopmentDemoTenantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed ISO country catalog");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
