using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Consumers;

/// <summary>
/// Executes the handlers of a delivered message exactly once per message id. The inbox check and the
/// handler run share the same scope so a handler failure leaves the message unprocessed and retryable.
/// </summary>
public sealed class IntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventRegistry registry,
    IIntegrationEventSerializer serializer,
    ILogger<IntegrationEventDispatcher> logger)
{
    private static readonly ConcurrentDictionary<Type, IHandlerInvoker> Invokers = new();

    public async Task<DispatchOutcome> DispatchAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (!registry.TryResolve(envelope.EventName, out Type eventType))
        {
            logger.LogWarning("No handler is registered for event {EventName}", envelope.EventName);
            return DispatchOutcome.Unhandled;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IInboxRepository? inbox = scope.ServiceProvider.GetService<IInboxRepository>();

        if (inbox is not null && await inbox.HasBeenProcessedAsync(envelope.MessageId, cancellationToken))
        {
            logger.LogDebug("Message {MessageId} was already processed", envelope.MessageId);
            return DispatchOutcome.Duplicate;
        }

        ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();

        using IDisposable tenantScope = currentTenant.Change(envelope.TenantId);

        IIntegrationEvent integrationEvent = serializer.Deserialize(envelope, eventType);

        IHandlerInvoker invoker = Invokers.GetOrAdd(
            eventType,
            static type => (IHandlerInvoker)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(type))!);

        await invoker.InvokeAsync(scope.ServiceProvider, integrationEvent, cancellationToken);

        if (inbox is not null)
        {
            await inbox.MarkProcessedAsync(envelope.MessageId, envelope.EventName, cancellationToken);
        }

        return DispatchOutcome.Handled;
    }

    private interface IHandlerInvoker
    {
        Task InvokeAsync(IServiceProvider serviceProvider, IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TEvent> : IHandlerInvoker
        where TEvent : IIntegrationEvent
    {
        public async Task InvokeAsync(
            IServiceProvider serviceProvider,
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            foreach (IIntegrationEventHandler<TEvent> handler in
                serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            {
                await handler.HandleAsync((TEvent)integrationEvent, cancellationToken);
            }
        }
    }
}

public enum DispatchOutcome
{
    Handled = 0,
    Duplicate = 1,
    Unhandled = 2
}
