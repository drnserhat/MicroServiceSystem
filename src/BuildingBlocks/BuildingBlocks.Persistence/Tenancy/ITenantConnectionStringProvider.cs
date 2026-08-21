namespace MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;

/// <summary>
/// Resolves a concrete Npgsql connection string for the ambient tenant + service key.
/// Implementations must never accept client-supplied connection strings.
/// </summary>
public interface ITenantConnectionStringProvider
{
    Task<string> ResolveAsync(Guid tenantId, string serviceKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// LRU / idle-evicting cache of Npgsql data sources keyed by tenant + service.
/// </summary>
public interface INpgsqlDataSourceCache
{
    Npgsql.NpgsqlDataSource GetOrAdd(Guid tenantId, string serviceKey, string connectionString);

    void Remove(Guid tenantId, string serviceKey);

    void RemoveAllForTenant(Guid tenantId);
}
