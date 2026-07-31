using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.Services.Identity.Infrastructure;

/// <summary>
/// Ensures the README demo tenant exists in Development so local registration/login samples work
/// against a real catalog row instead of an invented GUID.
/// </summary>
public sealed class DevelopmentTenantSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DevelopmentTenantSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            if (await tenants.GetByIdAsync(KnownTenants.DevelopmentDemo, cancellationToken) is not null)
            {
                return;
            }

            await tenants.AddAsync(
                Tenant.Provision(KnownTenants.DevelopmentDemo, "Development Demo", "dev-demo"),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded development demo tenant {TenantId}",
                KnownTenants.DevelopmentDemo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed development demo tenant");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
