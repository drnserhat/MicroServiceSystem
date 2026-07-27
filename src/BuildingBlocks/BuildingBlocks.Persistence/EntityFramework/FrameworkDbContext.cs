using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.DomainEvents;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Base context for every relational service database. It owns three cross-cutting guarantees:
/// tenant and soft delete query filters, transactional save semantics and domain event dispatch after
/// the transaction commits.
/// </summary>
public abstract class FrameworkDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    protected FrameworkDbContext(DbContextOptions options, DbContextDependencies dependencies)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        _currentTenant = dependencies.CurrentTenant;
        _domainEventDispatcher = dependencies.DomainEventDispatcher;
    }

    protected FrameworkDbContext(
        DbContextOptions options,
        ICurrentTenant currentTenant,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _currentTenant = currentTenant;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>
    /// Referenced by the generated global query filters; must stay public for expression compilation.
    /// </summary>
    public Guid? CurrentTenantId => _currentTenant.Id;

    /// <summary>
    /// Escape hatch for administrative reads such as restore or purge flows.
    /// </summary>
    public bool IncludeSoftDeleted { get; private set; }

    protected abstract string Schema { get; }

    public IDisposable AllowSoftDeletedResults()
    {
        IncludeSoftDeleted = true;

        return new SoftDeleteScope(this);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IDomainEvent> domainEvents = CollectDomainEvents();

        int affectedRows;

        try
        {
            affectedRows = await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The record was modified by another request. Reload it and try again.",
                exception);
        }

        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return affectedRows;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async token =>
        {
            await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(token);

            TResult result = await operation(token);

            await transaction.CommitAsync(token);

            return result;
        }, cancellationToken);
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteInTransactionAsync<object?>(async token =>
        {
            await operation(token);

            return null;
        }, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            LambdaExpression? filter = QueryFilterBuilder.Build(entityType.ClrType, this);

            if (filter is not null)
            {
                entityType.SetQueryFilter(filter);
            }
        }
    }

    private IReadOnlyList<IDomainEvent> CollectDomainEvents()
    {
        List<EntityEntry<IHasDomainEvents>> entries = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .ToList();

        List<IDomainEvent> domainEvents = entries
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        foreach (EntityEntry<IHasDomainEvents> entry in entries)
        {
            entry.Entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    private sealed class SoftDeleteScope(FrameworkDbContext context) : IDisposable
    {
        public void Dispose() => context.IncludeSoftDeleted = false;
    }
}
