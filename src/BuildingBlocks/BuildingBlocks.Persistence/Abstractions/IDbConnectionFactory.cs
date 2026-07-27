using System.Data.Common;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;

/// <summary>
/// Opens raw connections for read heavy Dapper queries that would be inefficient through the ORM.
/// </summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
