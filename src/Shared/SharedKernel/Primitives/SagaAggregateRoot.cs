using MicroServiceSystem.SharedKernel.Guards;

namespace MicroServiceSystem.SharedKernel.Primitives;

/// <summary>
/// Tenant-scoped aggregate base for long-running orchestrated processes.
/// Concrete sagas own their state enum and transition methods; this type
/// centralizes the shared <see cref="State"/> / <see cref="FailureReason"/> shape.
/// </summary>
public abstract class SagaAggregateRoot<TState> : TenantAggregateRoot<Guid>
    where TState : struct, Enum
{
    protected SagaAggregateRoot(Guid id)
        : base(id)
    {
    }

    protected SagaAggregateRoot()
    {
    }

    public TState State { get; private set; }

    public string? FailureReason { get; private set; }

    /// <summary>
    /// When the current owner's claim expires. Recovery only touches a saga whose lease has lapsed, which
    /// is what distinguishes an abandoned saga from one that is merely slow.
    /// </summary>
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    /// <summary>Identifies the process that currently owns the saga; diagnostic only.</summary>
    public string? LockedBy { get; private set; }

    /// <summary>True when the saga reached a terminal success or failure state.</summary>
    public abstract bool IsTerminal { get; }

    public bool IsLeaseExpired(DateTimeOffset utcNow) =>
        LockedUntilUtc is not { } until || until <= utcNow;

    /// <summary>
    /// Claims the saga until <paramref name="utcNow"/> plus <paramref name="leaseDuration"/>. Callers must
    /// still rely on the store's concurrency token to decide who actually won the claim.
    /// </summary>
    public void AcquireLease(string owner, DateTimeOffset utcNow, TimeSpan leaseDuration)
    {
        Ensure.NotNullOrWhiteSpace(owner);

        LockedBy = owner.Length > 128 ? owner[..128] : owner;
        LockedUntilUtc = utcNow.Add(leaseDuration);
    }

    public void RenewLease(DateTimeOffset utcNow, TimeSpan leaseDuration) =>
        LockedUntilUtc = utcNow.Add(leaseDuration);

    public void ReleaseLease()
    {
        LockedUntilUtc = null;
        LockedBy = null;
    }

    protected void TransitionTo(TState state) => State = state;

    protected void BeginCompensation(TState compensatingState, string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);
        FailureReason = reason.Trim();
        State = compensatingState;
    }

    protected void Fail(TState failedState, string reason)
    {
        Ensure.NotNullOrWhiteSpace(reason);
        FailureReason = reason.Trim();
        State = failedState;
        ReleaseLease();
    }

    protected void Complete(TState completedState)
    {
        State = completedState;
        ReleaseLease();
    }
}
