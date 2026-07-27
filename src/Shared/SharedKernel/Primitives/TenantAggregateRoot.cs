using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.SharedKernel.Primitives;

/// <summary>
/// Default aggregate base for tenant-scoped data: auditing, soft delete and tenant isolation are
/// applied by persistence interceptors and global query filters.
/// </summary>
public abstract class TenantAggregateRoot<TId> : SoftDeleteAggregateRoot<TId>, ITenantEntity
    where TId : notnull
{
    protected TenantAggregateRoot(TId id)
        : base(id)
    {
    }

    protected TenantAggregateRoot()
    {
    }

    public Guid TenantId { get; set; }
}
