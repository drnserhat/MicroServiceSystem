using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Persistence.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Applies migrations using <see cref="PostgresOptions.ConnectionString"/> even when the runtime
/// DbContext is tenant-scoped (bootstrap / template database only).
/// </summary>
public sealed class SharedConnectionMigrationService<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<SharedConnectionMigrationService<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        PostgresOptions postgresOptions = scope.ServiceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;
        DbContextDependencies dependencies = scope.ServiceProvider.GetRequiredService<DbContextDependencies>();

        DbContextOptionsBuilder<TContext> builder = new();
        builder.UseNpgsql(postgresOptions.ConnectionString, ConfigureNpgsql(postgresOptions));

        await using TContext context = (TContext)Activator.CreateInstance(
            typeof(TContext),
            builder.Options,
            dependencies)!;

        if (!context.Database.GetMigrations().Any())
        {
            logger.LogWarning(
                "No EF migrations found for {Context}; creating schema with EnsureCreated (shared connection)",
                typeof(TContext).Name);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        logger.LogInformation(
            "Applying pending migrations for {Context} on shared bootstrap connection",
            typeof(TContext).Name);
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Shared-connection migrations for {Context} are up to date", typeof(TContext).Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Action<NpgsqlDbContextOptionsBuilder> ConfigureNpgsql(PostgresOptions options) =>
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", options.Schema);
        };
}
