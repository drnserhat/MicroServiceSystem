using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.SharedKernel.Primitives;

public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
    where TId : notnull
{
    protected AuditableAggregateRoot(TId id)
        : base(id)
    {
    }

    protected AuditableAggregateRoot()
    {
    }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
