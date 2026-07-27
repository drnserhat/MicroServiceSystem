using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using MicroServiceSystem.BuildingBlocks.Authentication.Extensions;
using MicroServiceSystem.BuildingBlocks.Authorization.Extensions;
using MicroServiceSystem.BuildingBlocks.Caching.Extensions;
using MicroServiceSystem.BuildingBlocks.HealthChecks.Extensions;
using MicroServiceSystem.BuildingBlocks.Idempotency.Extensions;
using MicroServiceSystem.BuildingBlocks.Localization.Extensions;
using MicroServiceSystem.BuildingBlocks.Logging.Extensions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Extensions;
using MicroServiceSystem.BuildingBlocks.OpenTelemetry.Extensions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Configuration;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.ExceptionHandling;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Middleware;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Opinionated host bootstrap used by every service. Optional infrastructure such as Redis,
    /// RabbitMQ and Postgres stays in the service Infrastructure/Persistence layers so a lightweight
    /// service is not forced to take every dependency.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Services.AddOptions<ServiceDefaultsOptions>()
            .Bind(builder.Configuration.GetSection(ServiceDefaultsOptions.SectionName))
            .ValidateOnStart();

        ServiceDefaultsOptions defaults = builder.Configuration
            .GetSection(ServiceDefaultsOptions.SectionName)
            .Get<ServiceDefaultsOptions>() ?? new ServiceDefaultsOptions { ServiceName = serviceName };

        if (string.IsNullOrWhiteSpace(defaults.ServiceName))
        {
            defaults.ServiceName = serviceName;
        }

        builder.AddFrameworkLogging(serviceName);
        builder.AddFrameworkOpenTelemetry(serviceName);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"));
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        if (defaults.EnableSwagger)
        {
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = defaults.ServiceName,
                    Version = defaults.DefaultApiVersion,
                    Description = defaults.ServiceDescription
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
        }

        builder.Services.AddFrameworkAuthentication(builder.Configuration);
        builder.Services.AddFrameworkAuthorization();
        builder.Services.AddMultiTenancy(builder.Configuration);
        builder.Services.AddFrameworkCaching(builder.Configuration);
        builder.Services.AddFrameworkHealthChecks(builder.Configuration);

        if (defaults.EnableLocalization)
        {
            builder.Services.AddFrameworkLocalization(builder.Configuration);
        }

        if (defaults.EnableIdempotency)
        {
            builder.Services.AddFrameworkIdempotency(builder.Configuration);
        }

        if (defaults.EnableResponseCompression)
        {
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyOptions.PolicyName, policy =>
            {
                if (defaults.Cors.AllowedOrigins.Length == 0)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();

                    return;
                }

                policy.WithOrigins(defaults.Cors.AllowedOrigins);

                if (defaults.Cors.AllowedHeaders.Length == 0)
                {
                    policy.AllowAnyHeader();
                }
                else
                {
                    policy.WithHeaders(defaults.Cors.AllowedHeaders);
                }

                if (defaults.Cors.AllowedMethods.Length == 0)
                {
                    policy.AllowAnyMethod();
                }
                else
                {
                    policy.WithMethods(defaults.Cors.AllowedMethods);
                }

                if (defaults.Cors.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        });

        if (defaults.EnableRateLimiting)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(RateLimitingOptions.GlobalPolicyName, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = defaults.RateLimiting.PermitLimit,
                            Window = TimeSpan.FromSeconds(defaults.RateLimiting.WindowSeconds),
                            SegmentsPerWindow = defaults.RateLimiting.SegmentsPerWindow,
                            QueueLimit = defaults.RateLimiting.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = defaults.RateLimiting.PermitLimit,
                            Window = TimeSpan.FromSeconds(defaults.RateLimiting.WindowSeconds),
                            SegmentsPerWindow = defaults.RateLimiting.SegmentsPerWindow,
                            QueueLimit = defaults.RateLimiting.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
            });
        }

        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ServiceDefaultsOptions defaults = app.Configuration
            .GetSection(ServiceDefaultsOptions.SectionName)
            .Get<ServiceDefaultsOptions>() ?? new ServiceDefaultsOptions();

        app.UseExceptionHandler();
        app.UseFrameworkRequestLogging();

        if (defaults.EnableResponseCompression)
        {
            app.UseResponseCompression();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors(CorsPolicyOptions.PolicyName);

        if (defaults.EnableRateLimiting)
        {
            app.UseRateLimiter();
        }

        if (defaults.EnableLocalization)
        {
            app.UseFrameworkLocalization();
        }

        if (defaults.EnableSwagger && app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{defaults.ServiceName} v1");
                options.RoutePrefix = ApiRoutes.SwaggerRoutePrefix;
            });
        }

        app.UseAuthentication();
        app.UseMultiTenancy();
        app.UseAuthorization();

        if (defaults.EnableIdempotency)
        {
            app.UseFrameworkIdempotency();
        }

        app.MapFrameworkHealthChecks();
        app.MapFrameworkMetrics();

        return app;
    }
}
