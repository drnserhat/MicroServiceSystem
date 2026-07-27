using Microsoft.AspNetCore.Http;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;

public interface ITenantResolver
{
    TenantResolutionStrategy Strategy { get; }

    Task<TenantInfo?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
