using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;

public sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When true a request without a resolvable tenant is rejected instead of falling back to the
    /// default tenant. Endpoints explicitly marked as tenant independent are never rejected.
    /// </summary>
    public bool RequireTenant { get; set; } = true;

    public string HeaderName { get; set; } = FrameworkHeaders.TenantId;

    public string ClaimType { get; set; } = FrameworkClaimTypes.TenantId;

    public Guid DefaultTenantId { get; set; } = Guid.Empty;

    public TenantResolutionStrategy[] ResolutionOrder { get; set; } =
    [
        TenantResolutionStrategy.Claim,
        TenantResolutionStrategy.Header
    ];

    /// <summary>
    /// Trusting the tenant header is only safe behind the gateway, which validates the caller before
    /// forwarding. Direct public exposure must keep this disabled.
    /// </summary>
    public bool TrustTenantHeader { get; set; }
}
