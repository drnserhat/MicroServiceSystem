using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.SharedKernel.Primitives;

public abstract class SoftDeleteAggregateRoot<TId> : AuditableAggregateRoot<TId>, ISoftDeletable
    where TId : notnull
{
    protected SoftDeleteAggregateRoot(TId id)
        : base(id)
    {
    }

    protected SoftDeleteAggregateRoot()
    {
    }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }
}
