using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MicroServiceSystem.BuildingBlocks.Logging.Configuration;
using MicroServiceSystem.BuildingBlocks.Logging.Enrichers;
using Serilog;
using Serilog.Events;

namespace MicroServiceSystem.BuildingBlocks.Logging.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// Replaces the default logging stack with Serilog. Sinks are chosen from configuration so a
    /// service can ship logs to console, Seq or MongoDB without a code change.
    /// </summary>
    public static IHostApplicationBuilder AddFrameworkLogging(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Services.AddOptions<FrameworkLoggingOptions>()
            .Bind(builder.Configuration.GetSection(FrameworkLoggingOptions.SectionName))
            .ValidateOnStart();

        FrameworkLoggingOptions loggingOptions = builder.Configuration
            .GetSection(FrameworkLoggingOptions.SectionName)
            .Get<FrameworkLoggingOptions>() ?? new FrameworkLoggingOptions();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSerilog((serviceProvider, configuration) =>
        {
            configuration
                .MinimumLevel.Is(ParseLevel(loggingOptions.MinimumLevel))
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.With(new TenantEnricher(serviceProvider))
                .Enrich.With(new UserEnricher(serviceProvider.GetRequiredService<IHttpContextAccessor>()))
                .Enrich.WithProperty("ServiceName", serviceName)
                .ReadFrom.Configuration(builder.Configuration);

            if (loggingOptions.WriteToConsole)
            {
                configuration.WriteTo.Console();
            }

            if (!string.IsNullOrWhiteSpace(loggingOptions.SeqServerUrl))
            {
                configuration.WriteTo.Seq(
                    loggingOptions.SeqServerUrl,
                    apiKey: string.IsNullOrWhiteSpace(loggingOptions.SeqApiKey) ? null : loggingOptions.SeqApiKey);
            }

            if (!string.IsNullOrWhiteSpace(loggingOptions.MongoConnectionString))
            {
                configuration.WriteTo.MongoDBBson(
                    loggingOptions.MongoConnectionString,
                    loggingOptions.MongoCollectionName);
            }
        });

        return builder;
    }

    /// <summary>
    /// Correlation must be established before request logging so the access log carries the same id as
    /// everything the handler writes.
    /// </summary>
    public static IApplicationBuilder UseFrameworkRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
            };
        });

        return app;
    }

    private static LogEventLevel ParseLevel(string minimumLevel) =>
        Enum.TryParse(minimumLevel, ignoreCase: true, out LogEventLevel level)
            ? level
            : LogEventLevel.Information;
}
