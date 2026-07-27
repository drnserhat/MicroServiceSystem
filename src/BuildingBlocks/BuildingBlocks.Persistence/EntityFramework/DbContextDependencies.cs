using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Bundles the ambient services every <see cref="FrameworkDbContext"/> needs so derived contexts keep
/// a short constructor signature.
/// </summary>
public class DbContextDependencies(
    ICurrentTenant currentTenant,
    IDomainEventDispatcher domainEventDispatcher)
{
    public ICurrentTenant CurrentTenant { get; } = currentTenant;

    public IDomainEventDispatcher DomainEventDispatcher { get; } = domainEventDispatcher;
}
