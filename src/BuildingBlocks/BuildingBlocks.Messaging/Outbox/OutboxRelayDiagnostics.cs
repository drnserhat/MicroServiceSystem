namespace MicroServiceSystem.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Tracks whether the outbox relay is actually draining. A relay that throws every cycle would otherwise
/// only produce log lines while the service keeps reporting itself healthy and no event ever leaves.
/// The dead-letter backlog is refreshed each successful cycle so the health check can see poison rows
/// without querying the database on every probe.
/// </summary>
public sealed class OutboxRelayDiagnostics
{
    private readonly Lock _gate = new();

    public DateTimeOffset? LastSuccessUtc { get; private set; }

    public DateTimeOffset? LastFailureUtc { get; private set; }

    public string? LastError { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public int DeadLetterBacklog { get; private set; }

    public void RecordSuccess(DateTimeOffset timestampUtc)
    {
        lock (_gate)
        {
            LastSuccessUtc = timestampUtc;
            ConsecutiveFailures = 0;
            LastError = null;
        }
    }

    public void RecordFailure(DateTimeOffset timestampUtc, string error)
    {
        lock (_gate)
        {
            LastFailureUtc = timestampUtc;
            LastError = error;
            ConsecutiveFailures++;
        }
    }

    public void RecordDeadLetterBacklog(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        lock (_gate)
        {
            DeadLetterBacklog = count;
        }
    }

    public OutboxRelaySnapshot Snapshot()
    {
        lock (_gate)
        {
            return new OutboxRelaySnapshot(
                LastSuccessUtc,
                LastFailureUtc,
                LastError,
                ConsecutiveFailures,
                DeadLetterBacklog);
        }
    }
}

public readonly record struct OutboxRelaySnapshot(
    DateTimeOffset? LastSuccessUtc,
    DateTimeOffset? LastFailureUtc,
    string? LastError,
    int ConsecutiveFailures,
    int DeadLetterBacklog);
