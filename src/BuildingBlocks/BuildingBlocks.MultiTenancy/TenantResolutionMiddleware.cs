using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ICurrentTenant currentTenant,
        IEnumerable<ITenantResolver> resolvers,
        IOptions<MultiTenancyOptions> options)
    {
        MultiTenancyOptions multiTenancyOptions = options.Value;

        if (!multiTenancyOptions.Enabled || IsTenantIndependent(httpContext))
        {
            await next(httpContext);
            return;
        }

        TenantInfo? tenant = await ResolveTenantAsync(httpContext, resolvers, multiTenancyOptions);

        if (tenant is null && multiTenancyOptions.DefaultTenantId != Guid.Empty)
        {
            tenant = new TenantInfo(multiTenancyOptions.DefaultTenantId, "default");
        }

        if (tenant is null)
        {
            if (multiTenancyOptions.RequireTenant)
            {
                throw new TenantResolutionException("The request could not be associated with a tenant.");
            }

            await next(httpContext);
            return;
        }

        // Optional: only registered by hosts that validate tenants against a catalog.
        // Must not be an InvokeAsync parameter — DI treats those as required.
        ITenantStore? tenantStore = httpContext.RequestServices.GetService<ITenantStore>();

        if (tenantStore is not null)
        {
            TenantInfo? knownTenant = await tenantStore.FindAsync(tenant.Id, httpContext.RequestAborted);

            if (knownTenant is null || !knownTenant.IsActive)
            {
                throw new TenantResolutionException($"Tenant '{tenant.Id}' is unknown or inactive.");
            }

            tenant = knownTenant;
        }

        using IDisposable scope = currentTenant.Change(tenant.Id, tenant.Name);

        logger.LogDebug("Request scoped to tenant {TenantId}", tenant.Id);

        await next(httpContext);
    }

    private static bool IsTenantIndependent(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<ITenantIndependent>() is not null)
        {
            return true;
        }

        PathString path = httpContext.Request.Path;
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/metrics")
            || path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/docs");
    }

    private static async Task<TenantInfo?> ResolveTenantAsync(
        HttpContext httpContext,
        IEnumerable<ITenantResolver> resolvers,
        MultiTenancyOptions options)
    {
        ITenantResolver[] availableResolvers = [.. resolvers];

        foreach (TenantResolutionStrategy strategy in options.ResolutionOrder)
        {
            ITenantResolver? resolver = Array.Find(availableResolvers, candidate => candidate.Strategy == strategy);

            if (resolver is null)
            {
                continue;
            }

            TenantInfo? tenant = await resolver.ResolveAsync(httpContext, httpContext.RequestAborted);

            if (tenant is not null)
            {
                return tenant;
            }
        }

        return null;
    }
}
