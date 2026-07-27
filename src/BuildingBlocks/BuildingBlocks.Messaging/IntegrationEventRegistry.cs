using System.Reflection;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging;

/// <summary>
/// Derives the subscription list from the handlers a service actually implements, so a queue is never
/// bound to an event nobody consumes.
/// </summary>
public sealed class IntegrationEventRegistry : IIntegrationEventRegistry
{
    private readonly Dictionary<string, Type> _eventTypesByName;

    public IntegrationEventRegistry(IEnumerable<Assembly> handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        _eventTypesByName = handlerAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(@interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
            .Select(@interface => @interface.GetGenericArguments()[0])
            .Distinct()
            .ToDictionary(IntegrationEventNaming.Resolve, eventType => eventType, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> SubscribedEventNames => _eventTypesByName.Keys;

    public bool TryResolve(string eventName, out Type eventType) =>
        _eventTypesByName.TryGetValue(eventName, out eventType!);
}
