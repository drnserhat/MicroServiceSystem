using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence.Tenancy;

/// <summary>
/// Identity-owned catalog adapter for the multi-tenancy middleware and for handlers that accept a
/// caller-supplied tenant id.
/// </summary>
public sealed class EfTenantStore(ITenantRepository tenants) : ITenantStore
{
    public async Task<TenantInfo?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);

        return tenant is null
            ? null
            : new TenantInfo(tenant.Id, tenant.Name) { IsActive = tenant.IsActive };
    }
}
