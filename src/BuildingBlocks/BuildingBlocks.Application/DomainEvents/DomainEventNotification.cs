using MediatR;
using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.BuildingBlocks.Application.DomainEvents;

/// <summary>
/// Transports a domain event through MediatR without forcing the domain layer to depend on it.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
