using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Assigns the ambient tenant to new rows and refuses writes that would move a row to another tenant.
/// Without this guard a missing assignment would silently create cross-tenant data.
/// </summary>
public sealed class TenantAssignmentInterceptor(ICurrentTenant currentTenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null || currentTenant.Id is not { } tenantId)
        {
            return;
        }

        foreach (EntityEntry<ITenantEntity> entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity.TenantId == Guid.Empty:
                    entry.Entity.TenantId = tenantId;
                    break;

                case EntityState.Modified or EntityState.Deleted when entry.Entity.TenantId != tenantId:
                    throw new InvalidOperationException(
                        $"Entity '{entry.Metadata.ClrType.Name}' belongs to tenant '{entry.Entity.TenantId}' and cannot be modified by tenant '{tenantId}'.");

                default:
                    break;
            }
        }
    }
}
