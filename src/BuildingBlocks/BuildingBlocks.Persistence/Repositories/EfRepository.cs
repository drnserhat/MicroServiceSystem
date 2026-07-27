using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Specifications;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Repositories;

/// <summary>
/// Base class for aggregate specific repositories. It is deliberately abstract: services derive one
/// repository per aggregate root instead of injecting an open generic repository.
/// </summary>
public abstract class EfRepository<TAggregate, TId>(DbContext context) : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    protected DbContext Context { get; } = context;

    protected DbSet<TAggregate> Set => Context.Set<TAggregate>();

    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public virtual Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification)
            .FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TAggregate> query = SpecificationEvaluator.Apply(Set.AsQueryable(), specification);

        return await SpecificationEvaluator.ApplyPaging(query, specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TAggregate>> PagedListAsync(
        ISpecification<TAggregate> specification,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<TAggregate> query = SpecificationEvaluator.Apply(Set.AsQueryable(), specification);

        long totalCount = await query.LongCountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<TAggregate>.Empty(pagination);
        }

        List<TAggregate> items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<TAggregate>.Create(items, totalCount, pagination);
    }

    public virtual Task<long> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).LongCountAsync(cancellationToken);

    public virtual Task<bool> AnyAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).AnyAsync(cancellationToken);

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken) is not null;

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(aggregate, cancellationToken);

    public virtual async Task AddRangeAsync(
        IEnumerable<TAggregate> aggregates,
        CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(aggregates, cancellationToken);

    public virtual void Update(TAggregate aggregate) => Set.Update(aggregate);

    public virtual void Remove(TAggregate aggregate) => Set.Remove(aggregate);

    public virtual void RemoveRange(IEnumerable<TAggregate> aggregates) => Set.RemoveRange(aggregates);
}
