using System.Data.Common;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Configuration;
using Npgsql;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Dapper;

public sealed class NpgsqlConnectionFactory(IOptions<PostgresOptions> options) : IDbConnectionFactory
{
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(options.Value.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
