using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.SharedKernel.Primitives;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    protected AuditableEntity(TId id)
        : base(id)
    {
    }

    protected AuditableEntity()
    {
    }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
