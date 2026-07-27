using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Resolvers;

/// <summary>
/// Reads the tenant from the forwarded header. Only trusted when the service sits behind the gateway,
/// otherwise a caller could impersonate any tenant by setting a header.
/// </summary>
public sealed class HeaderTenantResolver(IOptions<MultiTenancyOptions> options) : ITenantResolver
{
    public TenantResolutionStrategy Strategy => TenantResolutionStrategy.Header;

    public Task<TenantInfo?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        MultiTenancyOptions multiTenancyOptions = options.Value;

        if (!multiTenancyOptions.TrustTenantHeader)
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        if (!httpContext.Request.Headers.TryGetValue(multiTenancyOptions.HeaderName, out StringValues headerValues))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        TenantInfo? tenant = Guid.TryParse(headerValues.ToString(), out Guid tenantId) && tenantId != Guid.Empty
            ? new TenantInfo(tenantId, tenantId.ToString())
            : null;

        return Task.FromResult(tenant);
    }
}
