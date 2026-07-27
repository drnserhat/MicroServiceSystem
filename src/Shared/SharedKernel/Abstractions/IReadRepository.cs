using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Specifications;

namespace MicroServiceSystem.SharedKernel.Abstractions;

/// <summary>
/// Read side of an aggregate scoped repository. Repositories are declared per aggregate root;
/// a service must not expose a single generic repository over unrelated aggregates.
/// </summary>
public interface IReadRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TAggregate>> PagedListAsync(
        ISpecification<TAggregate> specification,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
