using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MicroServiceSystem.BuildingBlocks.OpenTelemetry.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MicroServiceSystem.BuildingBlocks.OpenTelemetry.Extensions;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Instruments ASP.NET Core, outbound HTTP and the CLR runtime, then exports to OTLP and optionally
    /// scrapes Prometheus. Sampling is controlled from configuration so production can dial it down.
    /// </summary>
    public static IHostApplicationBuilder AddFrameworkOpenTelemetry(
        this IHostApplicationBuilder builder,
        string? serviceName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<TelemetryOptions>()
            .Bind(builder.Configuration.GetSection(TelemetryOptions.SectionName))
            .ValidateOnStart();

        TelemetryOptions options = builder.Configuration.GetSection(TelemetryOptions.SectionName)
            .Get<TelemetryOptions>() ?? new TelemetryOptions();

        string resolvedServiceName = string.IsNullOrWhiteSpace(serviceName)
            ? (string.IsNullOrWhiteSpace(options.ServiceName) ? builder.Environment.ApplicationName : options.ServiceName)
            : serviceName;

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: resolvedServiceName,
                    serviceVersion: options.ServiceVersion,
                    serviceInstanceId: Environment.MachineName));

        if (options.TracingEnabled)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(Math.Clamp(options.TraceSamplingRatio, 0, 1)))
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health")
                            && !httpContext.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(resolvedServiceName);

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });
        }

        if (options.MetricsEnabled)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(resolvedServiceName);

                if (options.PrometheusScrapingEnabled)
                {
                    metrics.AddPrometheusExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });
        }

        return builder;
    }

    public static WebApplication MapFrameworkMetrics(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        TelemetryOptions options = app.Configuration.GetSection(TelemetryOptions.SectionName)
            .Get<TelemetryOptions>() ?? new TelemetryOptions();

        if (options is { MetricsEnabled: true, PrometheusScrapingEnabled: true })
        {
            app.MapPrometheusScrapingEndpoint(options.PrometheusScrapingPath);
        }

        return app;
    }
}
