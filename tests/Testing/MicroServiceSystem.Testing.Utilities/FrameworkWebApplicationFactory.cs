using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        ["Authentication:Jwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
        ["Authentication:Jwt:RequireHttpsMetadata"] = "false",
        ["MultiTenancy:Enabled"] = "true",
        ["MultiTenancy:RequireTenant"] = "false",
        ["ServiceDefaults:EnableSwagger"] = "false",
        ["ServiceDefaults:EnableRateLimiting"] = "false",
        ["Cache:Provider"] = "Memory",
        ["Logging:Framework:WriteToConsole"] = "false"
    };

    public FrameworkWebApplicationFactory<TEntryPoint> WithSetting(string key, string? value)
    {
        _configuration[key] = value;

        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(_configuration);
        });
    }

    public T GetRequiredService<T>()
        where T : notnull =>
        Services.GetRequiredService<T>();
}
