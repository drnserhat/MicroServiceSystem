using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Applies pending migrations at startup. Intended for local and container Development only.
/// Production keeps <c>ApplyMigrationsOnStartup=false</c> and runs
/// <c>deploy/migrate/migrate-all.sh</c> (or the CI migrate job) before rolling out new images.
/// </summary>
public sealed class DatabaseMigrationService<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationService<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        TContext context = scope.ServiceProvider.GetRequiredService<TContext>();

        // Local/container boots often ship before the first checked-in migration exists. EnsureCreated
        // stands up the schema once; as soon as migrations appear, Migrate takes over.
        if (!context.Database.GetMigrations().Any())
        {
            logger.LogWarning(
                "No EF migrations found for {Context}; creating schema with EnsureCreated",
                typeof(TContext).Name);

            await context.Database.EnsureCreatedAsync(cancellationToken);

            return;
        }

        logger.LogInformation("Applying pending migrations for {Context}", typeof(TContext).Name);

        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Migrations for {Context} are up to date", typeof(TContext).Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
