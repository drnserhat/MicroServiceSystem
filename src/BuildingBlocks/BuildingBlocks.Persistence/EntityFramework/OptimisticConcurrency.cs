using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Reads and stamps the shadow <c>Version</c> concurrency token mapped by
/// <see cref="ConcurrencyConfigurationExtensions.UseOptimisticConcurrency{TEntity}"/>.
/// </summary>
public static class OptimisticConcurrency
{
    public static uint GetVersion<TEntity>(DbContext context, TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        return context.Entry(entity)
            .Property<uint>(ConcurrencyConfigurationExtensions.ConcurrencyTokenName)
            .CurrentValue;
    }

    /// <summary>
    /// Forces EF to compare against the client-supplied version on the next save so a stale
    /// <c>If-Match</c> fails with <see cref="SharedKernel.Primitives.ConcurrencyConflictException"/>.
    /// </summary>
    public static void SetExpectedVersion<TEntity>(DbContext context, TEntity entity, uint expectedVersion)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        PropertyEntry<TEntity, uint> property = context.Entry(entity)
            .Property<uint>(ConcurrencyConfigurationExtensions.ConcurrencyTokenName);

        property.OriginalValue = expectedVersion;
    }
}
