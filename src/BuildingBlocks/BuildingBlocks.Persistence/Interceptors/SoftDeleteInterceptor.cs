using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Turns physical deletes of <see cref="ISoftDeletable"/> aggregates into flag updates so history and
/// downstream projections stay intact.
/// </summary>
public sealed class SoftDeleteInterceptor(ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    : SaveChangesInterceptor
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
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry<ISoftDeletable> entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State is not EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = dateTimeProvider.UtcNow;
            entry.Entity.DeletedBy = currentUser.UserId?.ToString();
        }
    }
}
