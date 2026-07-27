using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Inbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Outbox;
using MicroServiceSystem.Contracts.Abstractions;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

[Collection(nameof(MessagingStoreCollection))]
public sealed class MessagingStoreConcurrencyTests(MessagingStoreFixture fixture)
{
    [Fact]
    public async Task Outbox_claim_skips_rows_locked_by_another_worker()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().AddRange(
                CreateOutbox(firstId, clock.UtcNow.AddMinutes(-2)),
                CreateOutbox(secondId, clock.UtcNow.AddMinutes(-1)));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext workerAContext = fixture.CreateContext();
        await using MessagingTestDbContext workerBContext = fixture.CreateContext();

        var workerA = new EfOutboxRepository<MessagingTestDbContext>(workerAContext, clock);
        var workerB = new EfOutboxRepository<MessagingTestDbContext>(workerBContext, clock);

        IReadOnlyList<IntegrationEventEnvelope> claimedByA = await workerA.ClaimPendingAsync(
            batchSize: 1,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-a",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        IReadOnlyList<IntegrationEventEnvelope> claimedByB = await workerB.ClaimPendingAsync(
            batchSize: 1,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-b",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        claimedByA.Count.ShouldBe(1);
        claimedByB.Count.ShouldBe(1);
        claimedByA[0].MessageId.ShouldBe(firstId);
        claimedByB[0].MessageId.ShouldBe(secondId);
        claimedByA[0].MessageId.ShouldNotBe(claimedByB[0].MessageId);
    }

    [Fact]
    public async Task Outbox_claim_reclaims_expired_leases()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventName = "tests.outbox.v1",
                Payload = "{}",
                OccurredOnUtc = clock.UtcNow.AddMinutes(-10),
                LockedUntilUtc = clock.UtcNow.AddMinutes(-1),
                LockedBy = "crashed-worker"
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfOutboxRepository<MessagingTestDbContext>(context, clock);

        IReadOnlyList<IntegrationEventEnvelope> claimed = await repository.ClaimPendingAsync(
            batchSize: 10,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "recovery-worker",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        claimed.Count.ShouldBe(1);
        claimed[0].MessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task Inbox_reserve_is_exclusive_until_lease_expires()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        await using MessagingTestDbContext context = fixture.CreateContext();
        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var repository = new EfInboxRepository<MessagingTestDbContext>(context, clock);
        Guid messageId = Guid.NewGuid();

        InboxReservationStatus first = await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        InboxReservationStatus second = await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        first.ShouldBe(InboxReservationStatus.Reserved);
        second.ShouldBe(InboxReservationStatus.Contended);
    }

    [Fact]
    public async Task Inbox_reports_duplicate_after_processed()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        await using MessagingTestDbContext context = fixture.CreateContext();
        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var repository = new EfInboxRepository<MessagingTestDbContext>(context, clock);
        Guid messageId = Guid.NewGuid();

        (await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken)).ShouldBe(InboxReservationStatus.Reserved);

        await repository.MarkProcessedAsync(messageId, "tests.inbox.v1", TestContext.Current.CancellationToken);

        (await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken)).ShouldBe(InboxReservationStatus.Duplicate);
    }

    [Fact]
    public async Task Inbox_allows_retry_after_mark_failed_releases_lock()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        await using MessagingTestDbContext context = fixture.CreateContext();
        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var repository = new EfInboxRepository<MessagingTestDbContext>(context, clock);
        Guid messageId = Guid.NewGuid();

        await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        await repository.MarkFailedAsync(
            messageId,
            "tests.inbox.v1",
            "boom",
            TestContext.Current.CancellationToken);

        (await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken)).ShouldBe(InboxReservationStatus.Reserved);
    }

    [Fact]
    public async Task Inbox_takes_over_stale_lease()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<InboxMessage>().Add(new InboxMessage
            {
                MessageId = messageId,
                EventName = "tests.inbox.v1",
                ReceivedOnUtc = clock.UtcNow.AddMinutes(-10),
                LockedUntilUtc = clock.UtcNow.AddMinutes(-1)
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfInboxRepository<MessagingTestDbContext>(context, clock);

        (await repository.TryReserveAsync(
            messageId,
            "tests.inbox.v1",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken)).ShouldBe(InboxReservationStatus.Reserved);
    }

    [Fact]
    public async Task Outbox_claim_returns_the_stored_envelope_fields()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventName = "tests.outbox.v1",
                Payload = """{"name":"value"}""",
                OccurredOnUtc = clock.UtcNow.AddMinutes(-1),
                TenantId = tenantId,
                CorrelationId = "correlation-1",
                TraceParent = "00-trace-span-01",
                Source = "tests",
                AttemptCount = 2
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfOutboxRepository<MessagingTestDbContext>(context, clock);

        IReadOnlyList<IntegrationEventEnvelope> claimed = await repository.ClaimPendingAsync(
            batchSize: 10,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-a",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        IntegrationEventEnvelope envelope = claimed.ShouldHaveSingleItem();
        envelope.MessageId.ShouldBe(messageId);
        envelope.EventName.ShouldBe("tests.outbox.v1");
        envelope.Payload.ShouldBe("""{"name":"value"}""");
        envelope.TenantId.ShouldBe(tenantId);
        envelope.CorrelationId.ShouldBe("correlation-1");
        envelope.TraceParent.ShouldBe("00-trace-span-01");
        envelope.Source.ShouldBe("tests");
        envelope.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task Outbox_claim_ignores_rows_that_exhausted_their_attempts()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventName = "tests.outbox.v1",
                Payload = "{}",
                OccurredOnUtc = clock.UtcNow.AddMinutes(-1),
                AttemptCount = 5
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfOutboxRepository<MessagingTestDbContext>(context, clock);

        IReadOnlyList<IntegrationEventEnvelope> claimed = await repository.ClaimPendingAsync(
            batchSize: 10,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-a",
            maxAttempts: 5,
            TestContext.Current.CancellationToken);

        claimed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Outbox_final_failure_dead_letters_the_row_and_stops_further_claims()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventName = "tests.outbox.v1",
                Payload = "{}",
                OccurredOnUtc = clock.UtcNow.AddMinutes(-1),
                AttemptCount = 4
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfOutboxRepository<MessagingTestDbContext>(context, clock);

        IReadOnlyList<IntegrationEventEnvelope> claimed = await repository.ClaimPendingAsync(
            batchSize: 1,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-a",
            maxAttempts: 5,
            TestContext.Current.CancellationToken);

        claimed.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);

        OutboxFailureOutcome outcome = await repository.MarkFailedAsync(
            messageId,
            "worker-a",
            "broker unreachable",
            maxAttempts: 5,
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(OutboxFailureOutcome.DeadLettered);

        OutboxMessage sealedRow = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(message => message.Id == messageId, TestContext.Current.CancellationToken);

        sealedRow.DeadLetteredOnUtc.ShouldNotBeNull();
        sealedRow.AttemptCount.ShouldBe(5);
        sealedRow.Error.ShouldBe("broker unreachable");
        sealedRow.LockedBy.ShouldBeNull();

        (await repository.CountDeadLetteredAsync(TestContext.Current.CancellationToken)).ShouldBe(1);

        (await repository.ClaimPendingAsync(
            batchSize: 10,
            leaseDuration: TimeSpan.FromMinutes(5),
            workerId: "worker-b",
            maxAttempts: 5,
            TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Outbox_seal_marks_pre_existing_exhausted_rows_as_dead_lettered()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            // The shape of poison rows that existed before DeadLetteredOnUtc: attempts exhausted,
            // never published, never sealed — invisible to ops and skipped by claim.
            seed.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventName = "tests.outbox.v1",
                Payload = "{}",
                OccurredOnUtc = clock.UtcNow.AddHours(-2),
                AttemptCount = 10,
                Error = "old failure"
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext context = fixture.CreateContext();
        var repository = new EfOutboxRepository<MessagingTestDbContext>(context, clock);

        (await repository.SealExhaustedAsync(10, TestContext.Current.CancellationToken)).ShouldBe(1);

        OutboxMessage sealedRow = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(message => message.Id == messageId, TestContext.Current.CancellationToken);

        sealedRow.DeadLetteredOnUtc.ShouldNotBeNull();
        sealedRow.DeadLetteredOnUtc!.Value.ShouldBe(clock.UtcNow, TimeSpan.FromSeconds(1));
        (await repository.CountDeadLetteredAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Outbox_completion_is_rejected_once_the_lease_moved_to_another_worker()
    {
        fixture.EnsureAvailable();
        await fixture.ResetAsync(TestContext.Current.CancellationToken);

        var clock = new MutableDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        Guid messageId = Guid.NewGuid();

        await using (MessagingTestDbContext seed = fixture.CreateContext())
        {
            seed.Set<OutboxMessage>().Add(CreateOutbox(messageId, clock.UtcNow.AddMinutes(-1)));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using MessagingTestDbContext slowWorkerContext = fixture.CreateContext();
        await using MessagingTestDbContext takeoverContext = fixture.CreateContext();

        var slowWorker = new EfOutboxRepository<MessagingTestDbContext>(slowWorkerContext, clock);
        var takeoverWorker = new EfOutboxRepository<MessagingTestDbContext>(takeoverContext, clock);

        await slowWorker.ClaimPendingAsync(
            batchSize: 1,
            leaseDuration: TimeSpan.FromSeconds(30),
            workerId: "slow-worker",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        // The slow worker is still publishing when its lease runs out and another relay takes the row.
        clock.UtcNow = clock.UtcNow.AddMinutes(1);

        await takeoverWorker.ClaimPendingAsync(
            batchSize: 1,
            leaseDuration: TimeSpan.FromSeconds(30),
            workerId: "takeover-worker",
            maxAttempts: 10,
            TestContext.Current.CancellationToken);

        bool completedBySlowWorker = await slowWorker.MarkPublishedAsync(
            messageId,
            "slow-worker",
            TestContext.Current.CancellationToken);

        completedBySlowWorker.ShouldBeFalse();

        bool completedByOwner = await takeoverWorker.MarkPublishedAsync(
            messageId,
            "takeover-worker",
            TestContext.Current.CancellationToken);

        completedByOwner.ShouldBeTrue();
    }

    private static OutboxMessage CreateOutbox(Guid id, DateTimeOffset occurredOnUtc) =>
        new()
        {
            Id = id,
            EventName = "tests.outbox.v1",
            Payload = "{}",
            OccurredOnUtc = occurredOnUtc
        };
}
