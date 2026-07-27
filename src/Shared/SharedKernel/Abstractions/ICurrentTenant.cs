namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface ICurrentTenant
{
    Guid? Id { get; }

    string? Name { get; }

    bool IsAvailable { get; }

    /// <summary>
    /// Switches the ambient tenant for the lifetime of the returned scope. Required by background
    /// workers and message consumers that process work on behalf of a tenant without an HTTP request.
    /// </summary>
    IDisposable Change(Guid? tenantId, string? tenantName = null);
}
