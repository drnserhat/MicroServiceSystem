namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

public sealed record TenantInfo(Guid Id, string Name)
{
    public bool IsActive { get; init; } = true;
}
