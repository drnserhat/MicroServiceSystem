namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;

/// <summary>
/// Optional lookup used to validate a resolved tenant and enrich it with its display name.
/// Services that do not own tenant metadata resolve tenants from the token only.
/// </summary>
public interface ITenantStore
{
    Task<TenantInfo?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
