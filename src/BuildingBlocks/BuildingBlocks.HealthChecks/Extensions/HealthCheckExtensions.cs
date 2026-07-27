using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MicroServiceSystem.BuildingBlocks.HealthChecks.Configuration;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.HealthChecks.Extensions;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddFrameworkHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FrameworkHealthCheckOptions>()
            .Bind(configuration.GetSection(FrameworkHealthCheckOptions.SectionName))
            .ValidateOnStart();

        IHealthChecksBuilder builder = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Live, HealthCheckTags.Ready]);

        string? postgres = configuration["Persistence:Postgres:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            builder.AddPostgresHealthCheck(postgres);
        }

        string? mongo = configuration["Persistence:Mongo:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(mongo))
        {
            builder.AddMongoHealthCheck(mongo);
        }

        string? cacheConnection = configuration["Cache:ConnectionString"];
        string? cacheProvider = configuration["Cache:Provider"];
        bool usesRedis = string.Equals(cacheProvider, "Redis", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cacheProvider, "Hybrid", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(cacheProvider) && !string.IsNullOrWhiteSpace(cacheConnection));

        if (usesRedis && !string.IsNullOrWhiteSpace(cacheConnection))
        {
            builder.AddRedisHealthCheck(cacheConnection);
        }

        string? rabbitHost = configuration["Messaging:RabbitMq:Host"];
        if (!string.IsNullOrWhiteSpace(rabbitHost))
        {
            builder.AddRabbitMqHealthCheck(BuildRabbitMqConnectionString(configuration));
        }

        return builder;
    }

    public static IHealthChecksBuilder AddPostgresHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "postgres")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.AddNpgSql(
            connectionString,
            name: name,
            tags: [HealthCheckTags.Ready, HealthCheckTags.Startup, HealthCheckTags.Database]);
    }

    public static IHealthChecksBuilder AddRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "redis")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.AddRedis(
            connectionString,
            name: name,
            tags: [HealthCheckTags.Ready, HealthCheckTags.Cache]);
    }

    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "rabbitmq")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.AddRabbitMQ(
            async _ =>
            {
                var factory = new ConnectionFactory { Uri = new Uri(connectionString) };

                return await factory.CreateConnectionAsync();
            },
            name: name,
            tags: [HealthCheckTags.Ready, HealthCheckTags.Broker]);
    }

    public static IHealthChecksBuilder AddMongoHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "mongodb")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.AddMongoDb(
            _ => new MongoClient(connectionString),
            name: name,
            tags: [HealthCheckTags.Ready, HealthCheckTags.Startup, HealthCheckTags.Database]);
    }

    public static WebApplication MapFrameworkHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        FrameworkHealthCheckOptions options = app.Configuration
            .GetSection(FrameworkHealthCheckOptions.SectionName)
            .Get<FrameworkHealthCheckOptions>() ?? new FrameworkHealthCheckOptions();

        MapAnonymousHealthCheck(app, options.LivenessPath, registration => registration.Tags.Contains(HealthCheckTags.Live));
        MapAnonymousHealthCheck(
            app,
            options.ReadinessPath,
            registration => registration.Tags.Contains(HealthCheckTags.Ready),
            new Dictionary<HealthStatus, int>
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            });
        MapAnonymousHealthCheck(app, options.StartupPath, registration => registration.Tags.Contains(HealthCheckTags.Startup));

        return app;
    }

    private static void MapAnonymousHealthCheck(
        WebApplication app,
        string path,
        Func<HealthCheckRegistration, bool> predicate,
        Dictionary<HealthStatus, int>? resultStatusCodes = null)
    {
        var options = new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = WriteResponseAsync
        };

        if (resultStatusCodes is not null)
        {
            foreach ((HealthStatus status, int code) in resultStatusCodes)
            {
                options.ResultStatusCodes[status] = code;
            }
        }

        app.MapHealthChecks(path, options).AllowAnonymous();
    }

    private static string BuildRabbitMqConnectionString(IConfiguration configuration)
    {
        string host = configuration["Messaging:RabbitMq:Host"] ?? "localhost";
        string port = configuration["Messaging:RabbitMq:Port"] ?? "5672";
        string userName = Uri.EscapeDataString(configuration["Messaging:RabbitMq:UserName"] ?? "guest");
        string password = Uri.EscapeDataString(configuration["Messaging:RabbitMq:Password"] ?? "guest");
        string virtualHost = configuration["Messaging:RabbitMq:VirtualHost"] ?? "/";
        string vhost = virtualHost == "/" ? string.Empty : Uri.EscapeDataString(virtualHost.TrimStart('/'));

        return $"amqp://{userName}:{password}@{host}:{port}/{vhost}";
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    error = entry.Value.Exception?.Message
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
