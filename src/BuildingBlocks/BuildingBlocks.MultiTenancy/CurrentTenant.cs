using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.MultiTenancy;

/// <summary>
/// Ambient tenant scope. The value flows with the async context so HTTP requests, message consumers
/// and background jobs all observe the same tenant without passing it through every signature.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private static readonly AsyncLocal<TenantScopeValue?> Current = new();

    public Guid? Id => Current.Value?.TenantId;

    public string? Name => Current.Value?.TenantName;

    public bool IsAvailable => Current.Value?.TenantId is not null;

    public IDisposable Change(Guid? tenantId, string? tenantName = null)
    {
        TenantScopeValue? previous = Current.Value;
        Current.Value = new TenantScopeValue(tenantId, tenantName);

        return new TenantScope(previous);
    }

    private sealed record TenantScopeValue(Guid? TenantId, string? TenantName);

    private sealed class TenantScope(TenantScopeValue? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Current.Value = previous;
            _disposed = true;
        }
    }
}
