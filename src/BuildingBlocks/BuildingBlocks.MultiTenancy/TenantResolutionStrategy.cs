namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

public enum TenantResolutionStrategy
{
    Claim = 0,
    Header = 1,
    Default = 2
}
