using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Messaging.Consumers;

/// <summary>
/// Executes the handlers of a delivered message exactly once per message id. Reservation happens
/// before handlers so concurrent deliveries cannot both execute side effects.
/// </summary>
public sealed class IntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventRegistry registry,
    IIntegrationEventSerializer serializer,
    IOptions<InboxOptions> inboxOptions,
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

        if (inbox is not null)
        {
            InboxReservationStatus reservation = await inbox.TryReserveAsync(
                envelope.MessageId,
                envelope.EventName,
                TimeSpan.FromSeconds(inboxOptions.Value.LockDurationSeconds),
                cancellationToken);

            switch (reservation)
            {
                case InboxReservationStatus.Duplicate:
                    logger.LogDebug("Message {MessageId} was already processed", envelope.MessageId);
                    return DispatchOutcome.Duplicate;

                case InboxReservationStatus.Contended:
                    logger.LogDebug(
                        "Message {MessageId} is reserved by another consumer",
                        envelope.MessageId);
                    return DispatchOutcome.Contended;
            }
        }

        ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();

        using IDisposable tenantScope = currentTenant.Change(envelope.TenantId);

        IIntegrationEvent integrationEvent = serializer.Deserialize(envelope, eventType);

        IHandlerInvoker invoker = Invokers.GetOrAdd(
            eventType,
            static type => (IHandlerInvoker)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(type))!);

        try
        {
            await invoker.InvokeAsync(scope.ServiceProvider, integrationEvent, cancellationToken);

            // Handlers only mutate aggregates; the unit of work behaviour that commits MediatR commands
            // does not run for them. Without this the tracked changes are dropped when the scope ends
            // while the inbox still records the message as processed, so the work is silently lost.
            IUnitOfWork? unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

            if (unitOfWork is not null)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (inbox is not null)
            {
                await inbox.MarkProcessedAsync(envelope.MessageId, envelope.EventName, cancellationToken);
            }

            return DispatchOutcome.Handled;
        }
        catch (Exception exception) when (inbox is not null)
        {
            await inbox.MarkFailedAsync(
                envelope.MessageId,
                envelope.EventName,
                exception.Message,
                cancellationToken);

            throw;
        }
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
    Unhandled = 2,
    Contended = 3
}
