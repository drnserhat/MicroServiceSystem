namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Maps wire event names to their contract types and to the queues this service consumes.
/// </summary>
public interface IIntegrationEventRegistry
{
    IReadOnlyCollection<string> SubscribedEventNames { get; }

    bool TryResolve(string eventName, out Type eventType);
}
