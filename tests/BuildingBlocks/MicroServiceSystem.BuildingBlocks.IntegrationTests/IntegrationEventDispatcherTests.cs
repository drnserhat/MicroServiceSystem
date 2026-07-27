using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Consumers;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class IntegrationEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_returns_unhandled_when_event_is_unknown()
    {
        IntegrationEventDispatcher dispatcher = CreateDispatcher(
            inbox: null,
            handler: null,
            registerHandler: false);

        DispatchOutcome outcome = await dispatcher.DispatchAsync(
            CreateEnvelope("unknown.event.v1"),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(DispatchOutcome.Unhandled);
    }

    [Fact]
    public async Task Dispatch_returns_duplicate_when_inbox_reports_duplicate()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Duplicate);

        var handler = new RecordingHandler();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(inbox, handler);

        DispatchOutcome outcome = await dispatcher.DispatchAsync(
            CreateEnvelope(),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(DispatchOutcome.Duplicate);
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Dispatch_returns_contended_when_inbox_is_locked()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Contended);

        var handler = new RecordingHandler();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(inbox, handler);

        DispatchOutcome outcome = await dispatcher.DispatchAsync(
            CreateEnvelope(),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(DispatchOutcome.Contended);
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Dispatch_marks_processed_after_successful_handler()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Reserved);

        var handler = new RecordingHandler();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(inbox, handler);
        IntegrationEventEnvelope envelope = CreateEnvelope();

        DispatchOutcome outcome = await dispatcher.DispatchAsync(
            envelope,
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(DispatchOutcome.Handled);
        handler.Calls.ShouldBe(1);
        await inbox.Received(1).MarkProcessedAsync(
            envelope.MessageId,
            envelope.EventName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_marks_failed_and_rethrows_when_handler_throws()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Reserved);

        var handler = new ThrowingHandler();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(inbox, handler);
        IntegrationEventEnvelope envelope = CreateEnvelope();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken));

        await inbox.Received(1).MarkFailedAsync(
            envelope.MessageId,
            envelope.EventName,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_commits_the_unit_of_work_before_marking_the_message_processed()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Reserved);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RecordingHandler();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(inbox, handler, unitOfWork: unitOfWork);
        IntegrationEventEnvelope envelope = CreateEnvelope();

        await dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Marking the inbox before the handler's changes are committed would suppress the retry that
        // is the only chance to persist them.
        Received.InOrder(() =>
        {
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            inbox.MarkProcessedAsync(envelope.MessageId, envelope.EventName, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Dispatch_does_not_commit_the_unit_of_work_when_the_handler_throws()
    {
        IInboxRepository inbox = Substitute.For<IInboxRepository>();
        inbox.TryReserveAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(InboxReservationStatus.Reserved);

        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IntegrationEventDispatcher dispatcher = CreateDispatcher(
            inbox,
            new ThrowingHandler(),
            unitOfWork: unitOfWork);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(CreateEnvelope(), TestContext.Current.CancellationToken));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static IntegrationEventDispatcher CreateDispatcher(
        IInboxRepository? inbox,
        IIntegrationEventHandler<DispatcherSampleEvent>? handler,
        bool registerHandler = true,
        IUnitOfWork? unitOfWork = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentTenant, CurrentTenant>();

        if (inbox is not null)
        {
            services.AddSingleton(inbox);
        }

        if (unitOfWork is not null)
        {
            services.AddSingleton(unitOfWork);
        }

        if (registerHandler && handler is not null)
        {
            services.AddSingleton(handler);
        }

        ServiceProvider provider = services.BuildServiceProvider();

        IIntegrationEventRegistry registry = registerHandler
            ? new IntegrationEventRegistry([typeof(RecordingHandler).Assembly])
            : Substitute.For<IIntegrationEventRegistry>();

        if (!registerHandler)
        {
            Type? ignored = null;
            registry.TryResolve(Arg.Any<string>(), out ignored).Returns(false);
        }

        return new IntegrationEventDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            new IntegrationEventSerializer(),
            Options.Create(new InboxOptions { LockDurationSeconds = 30 }),
            NullLogger<IntegrationEventDispatcher>.Instance);
    }

    private static IntegrationEventEnvelope CreateEnvelope(string eventName = "tests.dispatcher_sample.v1")
    {
        var sample = new DispatcherSampleEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOnUtc = DateTimeOffset.UtcNow,
            TenantId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString("N"),
            Value = "ok"
        };

        return new IntegrationEventSerializer().Serialize(sample, "tests") with { EventName = eventName };
    }

    [IntegrationEvent("tests.dispatcher_sample.v1")]
    private sealed record DispatcherSampleEvent : IntegrationEvent
    {
        public required string Value { get; init; }
    }

    private sealed class RecordingHandler : IIntegrationEventHandler<DispatcherSampleEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(DispatcherSampleEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            Calls += 1;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IIntegrationEventHandler<DispatcherSampleEvent>
    {
        public Task HandleAsync(DispatcherSampleEvent integrationEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("handler failed");
    }
}
