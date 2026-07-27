using System.Collections.Concurrent;
using MediatR;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.DomainEvents;

namespace MicroServiceSystem.BuildingBlocks.Application.DomainEvents;

public sealed class MediatorDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<IDomainEvent, INotification>> NotificationFactories = new();

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            INotification notification = CreateNotification(domainEvent);
            await publisher.Publish(notification, cancellationToken);
        }
    }

    private static INotification CreateNotification(IDomainEvent domainEvent)
    {
        Func<IDomainEvent, INotification> factory = NotificationFactories.GetOrAdd(
            domainEvent.GetType(),
            static eventType =>
            {
                Type notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                return @event => (INotification)Activator.CreateInstance(notificationType, @event)!;
            });

        return factory(domainEvent);
    }
}
