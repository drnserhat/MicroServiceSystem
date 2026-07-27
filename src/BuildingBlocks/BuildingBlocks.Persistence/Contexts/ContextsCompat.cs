using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Contexts;

/// <summary>
/// Compatibility facade so service templates can import Contexts instead of EntityFramework.
/// </summary>
public abstract class FrameworkDbContext : EntityFramework.FrameworkDbContext
{
    protected FrameworkDbContext(DbContextOptions options, DbContextDependencies dependencies)
        : base(options, dependencies)
    {
    }

    protected FrameworkDbContext(
        DbContextOptions options,
        ICurrentTenant currentTenant,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, currentTenant, domainEventDispatcher)
    {
    }
}

/// <summary>
/// Re-export of the EntityFramework dependency bundle under the Contexts namespace used by templates.
/// </summary>
public sealed class DbContextDependencies : EntityFramework.DbContextDependencies
{
    public DbContextDependencies(ICurrentTenant currentTenant, IDomainEventDispatcher domainEventDispatcher)
        : base(currentTenant, domainEventDispatcher)
    {
    }
}
