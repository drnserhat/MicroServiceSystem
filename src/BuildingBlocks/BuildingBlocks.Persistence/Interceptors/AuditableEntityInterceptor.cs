using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Stamps audit columns so no handler has to remember them. Runs for every context in the framework.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
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

        DateTimeOffset now = dateTimeProvider.UtcNow;
        string? actor = currentUser.UserId?.ToString();

        foreach (EntityEntry<IAuditableEntity> entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedBy = actor;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedBy = actor;
                    break;

                default:
                    if (entry.HasChangedOwnedEntities())
                    {
                        entry.Entity.ModifiedAtUtc = now;
                        entry.Entity.ModifiedBy = actor;
                    }

                    break;
            }
        }
    }
}

internal static class EntityEntryExtensions
{
    /// <summary>
    /// An aggregate whose owned value object changed stays Unchanged in the tracker, yet it is a real
    /// modification for auditing purposes.
    /// </summary>
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(reference =>
            reference.TargetEntry is { } target
            && target.Metadata.IsOwned()
            && target.State is EntityState.Added or EntityState.Modified);
}
