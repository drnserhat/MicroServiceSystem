using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Resolvers;

public sealed class ClaimTenantResolver(IOptions<MultiTenancyOptions> options) : ITenantResolver
{
    public TenantResolutionStrategy Strategy => TenantResolutionStrategy.Claim;

    public Task<TenantInfo?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        MultiTenancyOptions multiTenancyOptions = options.Value;

        string? value = httpContext.User.FindFirst(multiTenancyOptions.ClaimType)?.Value;

        TenantInfo? tenant = Guid.TryParse(value, out Guid tenantId) && tenantId != Guid.Empty
            ? new TenantInfo(tenantId, tenantId.ToString())
            : null;

        return Task.FromResult(tenant);
    }
}
