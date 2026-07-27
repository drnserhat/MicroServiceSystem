using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Configuration;
using MicroServiceSystem.BuildingBlocks.Persistence.Dapper;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Inbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Interceptors;
using MicroServiceSystem.BuildingBlocks.Persistence.Mongo;
using MicroServiceSystem.BuildingBlocks.Persistence.Outbox;
using MicroServiceSystem.SharedKernel.Abstractions;
using MongoDB.Driver;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using ContextsCompat = MicroServiceSystem.BuildingBlocks.Persistence.Contexts;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers a service DbContext against PostgreSQL together with the framework interceptors and
    /// the unit of work the application pipeline commits through.
    /// </summary>
    public static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
        where TContext : FrameworkDbContext
    {
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        PostgresOptions postgresOptions = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{PostgresOptions.SectionName}' is missing.");

        // Contexts facade is what templates inject; EntityFramework type is what real services inject.
        services.AddScoped<ContextsCompat.DbContextDependencies>();
        services.AddScoped<DbContextDependencies>(
            serviceProvider => serviceProvider.GetRequiredService<ContextsCompat.DbContextDependencies>());
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<TenantAssignmentInterceptor>();

        services.AddDbContext<TContext>((serviceProvider, builder) =>
        {
            builder.UseNpgsql(postgresOptions.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(postgresOptions.CommandTimeoutSeconds);
                npgsqlOptions.EnableRetryOnFailure(
                    postgresOptions.MaxRetryCount,
                    TimeSpan.FromSeconds(postgresOptions.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", postgresOptions.Schema);

                configureNpgsql?.Invoke(npgsqlOptions);
            });

            builder.AddInterceptors(
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<TenantAssignmentInterceptor>());

            builder.EnableSensitiveDataLogging(postgresOptions.EnableSensitiveDataLogging);
            builder.EnableDetailedErrors(postgresOptions.EnableDetailedErrors);
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TContext>());

        if (postgresOptions.ApplyMigrationsOnStartup)
        {
            services.AddHostedService<DatabaseMigrationService<TContext>>();
        }

        return services;
    }

    /// <summary>
    /// Template-friendly alias for <see cref="AddPostgresDbContext{TContext}"/>.
    /// </summary>
    public static IServiceCollection AddPostgresPersistence<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? schema = null,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
        where TContext : FrameworkDbContext
    {
        if (!string.IsNullOrWhiteSpace(schema))
        {
            services.PostConfigure<PostgresOptions>(options => options.Schema = schema);
        }

        return services.AddPostgresDbContext<TContext>(configuration, configureNpgsql);
    }

    /// <summary>
    /// Backs the messaging ports with the service database so events commit inside the business
    /// transaction.
    /// </summary>
    public static IServiceCollection AddEfMessagingStore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IOutboxWriter, EfOutboxWriter<TContext>>();
        services.AddScoped<IOutboxRepository, EfOutboxRepository<TContext>>();
        services.AddScoped<IInboxRepository, EfInboxRepository<TContext>>();

        return services;
    }

    public static IServiceCollection AddDapperAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }

    public static IServiceCollection AddMongoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        MongoOptions mongoOptions = configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{MongoOptions.SectionName}' is missing.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOptions.ConnectionString));
        services.AddSingleton<IMongoContext, MongoContext>();

        return services;
    }
}
