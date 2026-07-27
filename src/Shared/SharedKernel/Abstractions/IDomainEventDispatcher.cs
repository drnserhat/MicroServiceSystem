using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
