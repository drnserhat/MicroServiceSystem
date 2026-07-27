using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Minimal ambient services for <c>dotnet ef</c> design-time factories. Never used at runtime.
/// </summary>
public static class DesignTimeDbContextSupport
{
    public static DbContextDependencies CreateDependencies() =>
        new(new DesignTimeCurrentTenant(), new DesignTimeDomainEventDispatcher());

    private sealed class DesignTimeCurrentTenant : ICurrentTenant
    {
        public Guid? Id => null;

        public string? Name => null;

        public bool IsAvailable => false;

        public IDisposable Change(Guid? tenantId, string? tenantName = null) => NullScope.Instance;
    }

    private sealed class DesignTimeDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    protected abstract string DefaultConnectionString { get; }

    protected abstract TContext CreateNewInstance(
        DbContextOptions<TContext> options,
        DbContextDependencies dependencies);

    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(DefaultConnectionString);

        return CreateNewInstance(optionsBuilder.Options, DesignTimeDbContextSupport.CreateDependencies());
    }
}
