using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.TenantDatabases;
using MicroServiceSystem.Services.Identity.Domain;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.Services.Identity.Infrastructure;

/// <summary>
/// Seeds the local Postgres fleet cluster and a Ready binding for the demo tenant's shared <c>user</c> database.
/// </summary>
public sealed class DevelopmentBranchDatabaseSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<DevelopmentBranchDatabaseSeeder> logger) : IHostedService
{
    public static readonly Guid LocalClusterId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid DemoUserBindingId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IPostgresClusterRepository clusters = scope.ServiceProvider.GetRequiredService<IPostgresClusterRepository>();
            ITenantDatabaseBindingRepository bindings =
                scope.ServiceProvider.GetRequiredService<ITenantDatabaseBindingRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string host = configuration["BranchDatabase:DefaultHost"] ?? "postgres";

            PostgresCluster? cluster = await clusters.GetByIdAsync(LocalClusterId, cancellationToken);
            if (cluster is null)
            {
                cluster = PostgresCluster.Create(
                    name: "Local Compose",
                    slug: BranchDatabaseDefaults.DefaultClusterSlug,
                    host: host,
                    port: 5432,
                    adminSecretRef: BranchDatabaseDefaults.AdminConnectionSecretRef,
                    maxDatabases: 2000,
                    isDefault: true,
                    id: LocalClusterId);
                await clusters.AddAsync(cluster, cancellationToken);
                logger.LogInformation("Seeded default Postgres cluster {ClusterId} host={Host}", LocalClusterId, host);
            }

            TenantDatabaseBinding? binding =
                await bindings.FindByTenantAndServiceAsync(
                    KnownTenants.DevelopmentDemo,
                    KnownServiceKeys.User,
                    cancellationToken);

            if (binding is null)
            {
                binding = TenantDatabaseBinding.StartProvision(
                    KnownTenants.DevelopmentDemo,
                    KnownServiceKeys.User,
                    LocalClusterId,
                    databaseName: "user",
                    username: BranchDatabaseDefaults.AppUsername,
                    secretRef: BranchDatabaseDefaults.AppPasswordSecretRef,
                    id: DemoUserBindingId);
                binding.MarkReady("ef");
                await bindings.AddAsync(binding, cancellationToken);
                logger.LogInformation(
                    "Seeded Ready user DB binding for demo tenant {TenantId} → database 'user'",
                    KnownTenants.DevelopmentDemo);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed branch database catalog");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
