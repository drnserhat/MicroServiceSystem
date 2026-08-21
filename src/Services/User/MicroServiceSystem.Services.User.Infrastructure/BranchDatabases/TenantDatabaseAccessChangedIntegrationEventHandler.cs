using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;
using MicroServiceSystem.Contracts.Events.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace MicroServiceSystem.Services.User.Infrastructure.BranchDatabases;

public sealed class TenantDatabaseAccessChangedIntegrationEventHandler(
    INpgsqlDataSourceCache dataSourceCache,
    IMemoryCache memoryCache) : IIntegrationEventHandler<TenantDatabaseAccessChangedIntegrationEvent>
{
    public Task HandleAsync(
        TenantDatabaseAccessChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Guid tenantId = integrationEvent.BindingTenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = integrationEvent.TenantId ?? Guid.Empty;
        }

        if (tenantId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(integrationEvent.ServiceKey))
        {
            dataSourceCache.RemoveAllForTenant(tenantId);
        }
        else
        {
            dataSourceCache.Remove(tenantId, integrationEvent.ServiceKey);
            memoryCache.Remove($"tenant-db:{tenantId:N}:{integrationEvent.ServiceKey}");
        }

        return Task.CompletedTask;
    }
}
