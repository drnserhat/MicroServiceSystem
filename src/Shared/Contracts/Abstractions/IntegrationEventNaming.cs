using System.Collections.Concurrent;
using System.Reflection;

namespace MicroServiceSystem.Contracts.Abstractions;

public static class IntegrationEventNaming
{
    private static readonly ConcurrentDictionary<Type, string> NameCache = new();

    public static string Resolve<TEvent>()
        where TEvent : IIntegrationEvent =>
        Resolve(typeof(TEvent));

    public static string Resolve(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return NameCache.GetOrAdd(eventType, static type =>
        {
            IntegrationEventAttribute? attribute = type.GetCustomAttribute<IntegrationEventAttribute>();

            return attribute is null
                ? throw new InvalidOperationException(
                    $"Integration event '{type.FullName}' must declare an {nameof(IntegrationEventAttribute)}.")
                : attribute.Name;
        });
    }

    public static IReadOnlyDictionary<string, Type> BuildRegistry(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsClass: true } && type.IsAssignableTo(typeof(IIntegrationEvent)))
            .Where(type => type.GetCustomAttribute<IntegrationEventAttribute>() is not null)
            .ToDictionary(Resolve, type => type, StringComparer.Ordinal);
    }
}
