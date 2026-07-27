using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.Contracts.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Appends envelopes to the same <typeparamref name="TContext"/> the aggregate was changed in, which is
/// what makes state and messages commit atomically.
/// </summary>
public sealed class EfOutboxWriter<TContext>(TContext context) : IOutboxWriter
    where TContext : DbContext
{
    public async Task AppendAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        await context.Set<OutboxMessage>().AddAsync(ToMessage(envelope), cancellationToken);
    }

    public async Task AppendRangeAsync(
        IEnumerable<IntegrationEventEnvelope> envelopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        await context.Set<OutboxMessage>().AddRangeAsync(envelopes.Select(ToMessage), cancellationToken);
    }

    private static OutboxMessage ToMessage(IntegrationEventEnvelope envelope) =>
        new()
        {
            Id = envelope.MessageId,
            EventName = envelope.EventName,
            Payload = envelope.Payload,
            OccurredOnUtc = envelope.OccurredOnUtc,
            TenantId = envelope.TenantId,
            CorrelationId = envelope.CorrelationId,
            TraceParent = envelope.TraceParent,
            Source = envelope.Source,
            AttemptCount = envelope.AttemptCount
        };
}
