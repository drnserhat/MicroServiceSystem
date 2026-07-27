using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Consumers;
using MicroServiceSystem.BuildingBlocks.Messaging.Outbox;
using MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Extensions;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers the broker independent messaging surface plus the RabbitMQ transport. Outbox and inbox
    /// storage is intentionally left to the persistence layer of the hosting service.
    /// </summary>
    public static IServiceCollection AddFrameworkMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        params Assembly[] handlerAssemblies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<InboxOptions>()
            .Bind(configuration.GetSection(InboxOptions.SectionName))
            .ValidateOnStart();

        Assembly[] assemblies = handlerAssemblies.Length == 0
            ? [Assembly.GetCallingAssembly()]
            : handlerAssemblies;

        services.AddSingleton(new MessagingSource(serviceName));
        services.AddSingleton<MessagingTopology>();
        services.AddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.AddSingleton<IIntegrationEventRegistry>(_ => new IntegrationEventRegistry(assemblies));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<RabbitMqTopologyProvisioner>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IntegrationEventDispatcher>();

        services.TryAddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        RegisterHandlers(services, assemblies);

        return services;
    }

    /// <summary>
    /// Starts the outbox relay. Requires an <see cref="IOutboxRepository"/> registration from the
    /// persistence layer.
    /// </summary>
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessorService>();
        services.AddHostedService<OutboxCleanupService>();

        return services;
    }

    /// <summary>
    /// Template-friendly alias for <see cref="AddOutboxProcessor"/>.
    /// </summary>
    public static IServiceCollection AddOutboxProcessing(
        this IServiceCollection services,
        IConfiguration? configuration = null) =>
        services.AddOutboxProcessor();

    /// <summary>
    /// Template-friendly alias for <see cref="AddFrameworkMessaging"/>.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        params Assembly[] handlerAssemblies) =>
        services.AddFrameworkMessaging(configuration, serviceName, handlerAssemblies);

    /// <summary>
    /// Starts the RabbitMQ consumer for the events this service has handlers for.
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumers(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqConsumerService>();

        return services;
    }

    /// <summary>
    /// Template-friendly overload that also registers handlers from the given assemblies.
    /// Prefer registering handlers through <see cref="AddFrameworkMessaging"/> when possible.
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumers(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] handlerAssemblies)
    {
        _ = configuration;

        if (handlerAssemblies.Length > 0)
        {
            RegisterHandlers(services, handlerAssemblies);
        }

        return services.AddIntegrationEventConsumers();
    }

    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type implementation in assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }))
            {
                foreach (Type handlerInterface in implementation.GetInterfaces()
                    .Where(@interface => @interface.IsGenericType
                        && @interface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                {
                    services.AddScoped(handlerInterface, implementation);
                }
            }
        }
    }
}
