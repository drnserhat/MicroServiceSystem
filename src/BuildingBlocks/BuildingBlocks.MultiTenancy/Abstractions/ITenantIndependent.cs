namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;

/// <summary>
/// Marks an endpoint or a consumer as tenant independent, for example health probes, token issuance
/// and platform administration surfaces.
/// </summary>
public interface ITenantIndependent;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class TenantIndependentAttribute : Attribute, ITenantIndependent;
