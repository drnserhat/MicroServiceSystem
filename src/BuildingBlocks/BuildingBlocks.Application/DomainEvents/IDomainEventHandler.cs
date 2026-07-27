using MediatR;
using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.BuildingBlocks.Application.DomainEvents;

public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
