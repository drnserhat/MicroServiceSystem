using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Abstractions;

public interface ITenantDatabaseProvisioner
{
    /// <summary>
    /// Creates the database (if needed), asks the owning service to migrate, and updates binding status.
    /// </summary>
    Task<Result> ProvisionAsync(
        TenantDatabaseBinding binding,
        PostgresCluster cluster,
        CancellationToken cancellationToken = default);

    Task<Result> PingAsync(
        TenantDatabaseBinding binding,
        PostgresCluster cluster,
        CancellationToken cancellationToken = default);
}
