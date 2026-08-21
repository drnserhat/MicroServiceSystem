using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;
using Npgsql;

namespace MicroServiceSystem.Services.User.Infrastructure.BranchDatabases;

public sealed class IdentityCatalogOptions
{
    public const string SectionName = "Services:Identity";

    public string BaseUrl { get; set; } = "http://localhost:5080/";
}

public sealed class IdentityCatalogTenantConnectionStringProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<IdentityCatalogOptions> identityOptions,
    IOptions<InternalServiceOptions> internalServiceOptions,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<IdentityCatalogTenantConnectionStringProvider> logger) : ITenantConnectionStringProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> ResolveAsync(
        Guid tenantId,
        string serviceKey,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"tenant-db:{tenantId:N}:{serviceKey}";
        if (cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        HttpClient client = httpClientFactory.CreateClient("user-identity-catalog");
        client.BaseAddress = new Uri(identityOptions.Value.BaseUrl);

        InternalServiceOptions internalOptions = internalServiceOptions.Value;
        if (internalOptions.Enabled && !string.IsNullOrWhiteSpace(internalOptions.ApiKey))
        {
            client.DefaultRequestHeaders.Remove(internalOptions.HeaderName);
            client.DefaultRequestHeaders.TryAddWithoutValidation(internalOptions.HeaderName, internalOptions.ApiKey);
        }

        string path = $"api/v1/tenants/{tenantId}/databases/{serviceKey}/binding";
        using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Binding resolve failed for tenant {TenantId} service {ServiceKey}: {Status}",
                tenantId,
                serviceKey,
                (int)response.StatusCode);
            throw new InvalidOperationException(
                $"Unable to resolve tenant database binding ({(int)response.StatusCode}): {Truncate(body)}");
        }

        // Identity wraps payloads in ApiResponse<T> ({ succeeded, data, ... }).
        ApiEnvelope? envelope =
            await response.Content.ReadFromJsonAsync<ApiEnvelope>(JsonOptions, cancellationToken);
        BindingDto? binding = envelope?.Data;
        if (binding is null)
        {
            throw new InvalidOperationException("Identity returned an empty binding payload.");
        }

        string secretRef = string.IsNullOrWhiteSpace(binding.SecretRef)
            ? "Persistence:Postgres:AppPassword"
            : binding.SecretRef;
        string? password = configuration[secretRef];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"SecretRef '{secretRef}' is not configured.");
        }

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = binding.Host,
            Port = binding.Port,
            Database = binding.DatabaseName,
            Username = binding.Username,
            Password = password
        };

        string connectionString = builder.ConnectionString;
        cache.Set(cacheKey, connectionString, TimeSpan.FromMinutes(2));
        return connectionString;
    }

    private static string Truncate(string value) =>
        value.Length <= 300 ? value : value[..300];

    private sealed class ApiEnvelope
    {
        public bool Succeeded { get; set; }

        public BindingDto? Data { get; set; }
    }

    private sealed class BindingDto
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string DatabaseName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string SecretRef { get; set; } = string.Empty;
    }
}
