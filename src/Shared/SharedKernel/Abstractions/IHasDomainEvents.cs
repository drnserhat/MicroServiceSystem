using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
