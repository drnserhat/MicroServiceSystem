using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.Services.Identity.Application;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.TenantDatabases;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Results;
using Npgsql;

namespace MicroServiceSystem.Services.Identity.Infrastructure.BranchDatabases;

public sealed class UserServiceOptions
{
    public const string SectionName = "Services:User";

    public string BaseUrl { get; set; } = "http://localhost:5081/";
}

public sealed class TenantDatabaseProvisioner(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IOptions<UserServiceOptions> userServiceOptions,
    IOptions<InternalServiceOptions> internalServiceOptions,
    ILogger<TenantDatabaseProvisioner> logger) : ITenantDatabaseProvisioner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result> ProvisionAsync(
        TenantDatabaseBinding binding,
        PostgresCluster cluster,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? adminConnection = configuration[cluster.AdminSecretRef]
                ?? configuration[BranchDatabaseDefaults.AdminConnectionSecretRef];

            if (string.IsNullOrWhiteSpace(adminConnection))
            {
                return Result.Failure(Error.Failure(
                    "identity.admin_connection_missing",
                    $"Admin connection config '{cluster.AdminSecretRef}' is missing."));
            }

            await EnsureDatabaseExistsAsync(adminConnection, binding.DatabaseName, cancellationToken);

            binding.MarkMigrating();

            Result migrate = await RequestUserMigrateAsync(binding, cluster, cancellationToken);
            if (migrate.IsFailure)
            {
                return migrate;
            }

            binding.MarkReady(schemaVersion: "ef");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Provisioning tenant database {Database} for tenant {TenantId} failed",
                binding.DatabaseName,
                binding.TenantId);
            return Result.Failure(Error.Failure("identity.provision_exception", ex.Message));
        }
    }

    public async Task<Result> PingAsync(
        TenantDatabaseBinding binding,
        PostgresCluster cluster,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? adminConnection = configuration[cluster.AdminSecretRef]
                ?? configuration[BranchDatabaseDefaults.AdminConnectionSecretRef];

            if (string.IsNullOrWhiteSpace(adminConnection))
            {
                return Result.Failure(Error.Failure(
                    "identity.admin_connection_missing",
                    $"Admin connection config '{cluster.AdminSecretRef}' is missing."));
            }

            NpgsqlConnectionStringBuilder builder = new(adminConnection)
            {
                Database = binding.DatabaseName
            };

            await using NpgsqlConnection connection = new(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using NpgsqlCommand command = new("SELECT 1", connection);
            _ = await command.ExecuteScalarAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Health ping failed for tenant database {Database}",
                binding.DatabaseName);
            return Result.Failure(Error.Failure("identity.tenant_database_unhealthy", ex.Message));
        }
    }

    private static async Task EnsureDatabaseExistsAsync(
        string adminConnection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(adminConnection);
        await connection.OpenAsync(cancellationToken);

        await using (NpgsqlCommand exists = new(
            "SELECT 1 FROM pg_database WHERE datname = @name",
            connection))
        {
            exists.Parameters.AddWithValue("name", databaseName);
            object? found = await exists.ExecuteScalarAsync(cancellationToken);
            if (found is not null)
            {
                return;
            }
        }

        // Database names are sanitized to [a-z0-9_]; quote as identifier.
        string quoted = "\"" + databaseName.Replace("\"", string.Empty, StringComparison.Ordinal) + "\"";
        await using NpgsqlCommand create = new($"CREATE DATABASE {quoted}", connection);
        await create.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<Result> RequestUserMigrateAsync(
        TenantDatabaseBinding binding,
        PostgresCluster cluster,
        CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("identity-user-provision");
        client.BaseAddress = new Uri(userServiceOptions.Value.BaseUrl);

        InternalServiceOptions internalOptions = internalServiceOptions.Value;
        if (internalOptions.Enabled && !string.IsNullOrWhiteSpace(internalOptions.ApiKey))
        {
            client.DefaultRequestHeaders.Remove(internalOptions.HeaderName);
            client.DefaultRequestHeaders.TryAddWithoutValidation(internalOptions.HeaderName, internalOptions.ApiKey);
        }

        var payload = new
        {
            host = cluster.Host,
            port = cluster.Port,
            databaseName = binding.DatabaseName,
            username = binding.Username,
            secretRef = binding.SecretRef
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/v1/tenant-databases/ensure-migrated",
            payload,
            JsonOptions,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Failure(Error.Failure(
            "identity.user_migrate_failed",
            $"User migrate returned {(int)response.StatusCode}: {Truncate(body)}"));
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];
}
