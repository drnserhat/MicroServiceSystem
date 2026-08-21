using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Configuration;
using MicroServiceSystem.BuildingBlocks.Persistence.Dapper;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Inbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Interceptors;
using MicroServiceSystem.BuildingBlocks.Persistence.Mongo;
using MicroServiceSystem.BuildingBlocks.Persistence.Outbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;
using MicroServiceSystem.SharedKernel.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Npgsql;
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

        if (string.Equals(postgresOptions.Mode, PostgresOptions.ModeTenantScoped, StringComparison.OrdinalIgnoreCase))
        {
            return services.AddPostgresDbContextTenantScoped<TContext>(configuration, configureNpgsql);
        }

        RegisterSharedInfrastructure<TContext>(services, postgresOptions, configureNpgsql);
        return services;
    }

    /// <summary>
    /// Tenant-scoped DbContext: connection is resolved from ambient <see cref="ICurrentTenant"/> +
    /// <see cref="ITenantConnectionStringProvider"/> and pooled via <see cref="INpgsqlDataSourceCache"/>.
    /// </summary>
    public static IServiceCollection AddPostgresDbContextTenantScoped<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
        where TContext : FrameworkDbContext
    {
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NpgsqlDataSourceCacheOptions>()
            .Bind(configuration.GetSection(NpgsqlDataSourceCacheOptions.SectionName));

        PostgresOptions postgresOptions = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{PostgresOptions.SectionName}' is missing.");

        if (string.IsNullOrWhiteSpace(postgresOptions.ServiceKey))
        {
            throw new InvalidOperationException(
                $"{PostgresOptions.SectionName}:ServiceKey is required when Mode=TenantScoped.");
        }

        services.AddSingleton<INpgsqlDataSourceCache, LruNpgsqlDataSourceCache>();

        services.AddScoped<ContextsCompat.DbContextDependencies>();
        services.AddScoped<DbContextDependencies>(
            serviceProvider => serviceProvider.GetRequiredService<ContextsCompat.DbContextDependencies>());
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<TenantAssignmentInterceptor>();

        string serviceKey = postgresOptions.ServiceKey;

        services.AddDbContext<TContext>((serviceProvider, builder) =>
        {
            PostgresOptions options = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;
            ICurrentTenant currentTenant = serviceProvider.GetRequiredService<ICurrentTenant>();

            if (currentTenant.Id is null)
            {
                // Background outbox/inbox processors and [TenantIndependent] ops use the bootstrap
                // connection (Compose template DB). Branch OLTP still requires ambient tenant.
                builder.UseNpgsql(options.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                    npgsqlOptions.EnableRetryOnFailure(
                        options.MaxRetryCount,
                        TimeSpan.FromSeconds(options.MaxRetryDelaySeconds),
                        errorCodesToAdd: null);
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", options.Schema);

                    configureNpgsql?.Invoke(npgsqlOptions);
                });
            }
            else
            {
                ITenantConnectionStringProvider connectionProvider =
                    serviceProvider.GetRequiredService<ITenantConnectionStringProvider>();
                string connectionString = connectionProvider
                    .ResolveAsync(currentTenant.Id.Value, serviceKey)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                INpgsqlDataSourceCache cache = serviceProvider.GetRequiredService<INpgsqlDataSourceCache>();
                NpgsqlDataSource dataSource = cache.GetOrAdd(currentTenant.Id.Value, serviceKey, connectionString);

                builder.UseNpgsql(dataSource, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                    npgsqlOptions.EnableRetryOnFailure(
                        options.MaxRetryCount,
                        TimeSpan.FromSeconds(options.MaxRetryDelaySeconds),
                        errorCodesToAdd: null);
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", options.Schema);

                    configureNpgsql?.Invoke(npgsqlOptions);
                });
            }

            builder.AddInterceptors(
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<TenantAssignmentInterceptor>());

            builder.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
            builder.EnableDetailedErrors(options.EnableDetailedErrors);
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TContext>());

        // Bootstrap migrations still target Persistence:Postgres:ConnectionString (shared template DB).
        if (postgresOptions.ApplyMigrationsOnStartup)
        {
            services.AddHostedService<SharedConnectionMigrationService<TContext>>();
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

        // MongoDB.Driver 3.x refuses Guid filters/documents when representation is Unspecified.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOptions.ConnectionString));
        services.AddSingleton<IMongoContext, MongoContext>();

        return services;
    }

    private static void RegisterSharedInfrastructure<TContext>(
        IServiceCollection services,
        PostgresOptions postgresOptions,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql)
        where TContext : FrameworkDbContext
    {
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
    }
}
