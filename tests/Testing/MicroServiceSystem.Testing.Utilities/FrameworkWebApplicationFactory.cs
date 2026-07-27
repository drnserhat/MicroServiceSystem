using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MicroServiceSystem.SharedKernel.Security;

namespace MicroServiceSystem.Testing.Utilities;

/// <summary>
/// Shared WebApplicationFactory helpers for service integration tests. Concrete test projects inherit
/// and override configuration for their service under test.
/// </summary>
public class FrameworkWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly Dictionary<string, string?> _configuration = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Authentication:Jwt:Issuer"] = "msf-tests",
        ["Authentication:Jwt:Audience"] = "msf-tests",
        ["Authentication:Jwt:SigningKey"] = KnownInsecureSecrets.DevelopmentJwtSigningKey,
        ["Authentication:Jwt:RequireHttpsMetadata"] = "false",
        ["Authentication:InternalService:Enabled"] = "true",
        ["Authentication:InternalService:ApiKey"] = KnownInsecureSecrets.DevelopmentInternalApiKey,
        ["MultiTenancy:Enabled"] = "true",
        ["MultiTenancy:RequireTenant"] = "false",
        ["MultiTenancy:TrustTenantHeader"] = "false",
        ["ServiceDefaults:EnableSwagger"] = "false",
        ["ServiceDefaults:EnableRateLimiting"] = "false",
        ["ServiceDefaults:EnableIdempotency"] = "false",
        ["Cache:Provider"] = "Memory",
        ["Logging:Framework:WriteToConsole"] = "false",
        // Satisfy RabbitMqOptions.ValidateOnStart without needing a live broker: API tests strip the
        // relay/consumer hosted services so these credentials are never dialed.
        ["Messaging:RabbitMq:Host"] = "127.0.0.1",
        ["Messaging:RabbitMq:UserName"] = "test",
        ["Messaging:RabbitMq:Password"] = "test",
        ["Messaging:Outbox:Enabled"] = "false",
        ["Messaging:Inbox:Enabled"] = "false",
        ["Saga:RecoveryEnabled"] = "false"
    };

    private Action<IServiceCollection>? _configureServices;

    public FrameworkWebApplicationFactory<TEntryPoint> WithSetting(string key, string? value)
    {
        _configuration[key] = value;
        return this;
    }

    public FrameworkWebApplicationFactory<TEntryPoint> WithTestServices(Action<IServiceCollection> configure)
    {
        _configureServices += configure;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // UseSetting lands in the host configuration layer that wins over appsettings.*.json, which is
        // what keeps local Development connection strings from hijacking Testcontainer endpoints.
        foreach ((string key, string? value) in _configuration)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(_configuration);
        });

        builder.ConfigureTestServices(services =>
        {
            StripBrokerHostedServices(services);
            _configureServices?.Invoke(services);
        });
    }

    public T GetRequiredService<T>()
        where T : notnull =>
        Services.GetRequiredService<T>();

    /// <summary>
    /// Removes RabbitMQ consumers and outbox relays so API tests do not need a broker. Migration and
    /// Development seed hosted services are left alone.
    /// </summary>
    private static void StripBrokerHostedServices(IServiceCollection services)
    {
        ServiceDescriptor[] removable =
        [
            .. services.Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType is { } type
                && IsBrokerOrRelayHostedService(type))
        ];

        foreach (ServiceDescriptor descriptor in removable)
        {
            services.Remove(descriptor);
        }
    }

    private static bool IsBrokerOrRelayHostedService(Type type)
    {
        string name = type.Name;

        return name.Contains("Outbox", StringComparison.Ordinal)
            || name.Contains("RabbitMq", StringComparison.Ordinal)
            || name.Contains("SagaRecovery", StringComparison.Ordinal);
    }
}
