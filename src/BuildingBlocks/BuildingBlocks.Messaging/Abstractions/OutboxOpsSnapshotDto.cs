namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Shared ops response shape for per-service outbox endpoints (HTTP DTOs, not MVC controllers).
/// </summary>
public sealed record OutboxOpsSummaryDto(string Service, int PendingCount, int DeadLetterCount);

public sealed record OutboxOpsDeadLetterDto(
    Guid Id,
    string EventName,
    DateTimeOffset OccurredOnUtc,
    DateTimeOffset? DeadLetteredOnUtc,
    int AttemptCount,
    string? Error,
    Guid? TenantId,
    string? CorrelationId);

public sealed record OutboxOpsPendingDto(
    Guid Id,
    string EventName,
    DateTimeOffset OccurredOnUtc,
    int AttemptCount,
    Guid? TenantId,
    string? CorrelationId,
    DateTimeOffset? LockedUntilUtc);

public sealed record OutboxOpsSnapshotDto(
    OutboxOpsSummaryDto Summary,
    IReadOnlyList<OutboxOpsDeadLetterDto> DeadLetters,
    IReadOnlyList<OutboxOpsPendingDto> Pending);

public static class OutboxOpsSnapshotFactory
{
    public static OutboxOpsSnapshotDto Create(
        string service,
        int pendingCount,
        int deadLetterCount,
        IReadOnlyList<OutboxDeadLetterRow> deadLetters,
        IReadOnlyList<OutboxPendingRow> pending) =>
        new(
            new OutboxOpsSummaryDto(service, pendingCount, deadLetterCount),
            deadLetters.Select(row => new OutboxOpsDeadLetterDto(
                row.Id,
                row.EventName,
                row.OccurredOnUtc,
                row.DeadLetteredOnUtc,
                row.AttemptCount,
                row.Error,
                row.TenantId,
                row.CorrelationId)).ToArray(),
            pending.Select(row => new OutboxOpsPendingDto(
                row.Id,
                row.EventName,
                row.OccurredOnUtc,
                row.AttemptCount,
                row.TenantId,
                row.CorrelationId,
                row.LockedUntilUtc)).ToArray());
}
