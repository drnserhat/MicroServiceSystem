namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

public sealed class TenantResolutionException(string message) : Exception(message);
