namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface IRepository<TAggregate, TId> : IReadRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TAggregate> aggregates, CancellationToken cancellationToken = default);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);

    void RemoveRange(IEnumerable<TAggregate> aggregates);
}
