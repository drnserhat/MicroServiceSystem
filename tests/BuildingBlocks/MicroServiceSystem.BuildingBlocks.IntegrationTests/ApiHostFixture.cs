using Testcontainers.PostgreSql;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Shared Postgres for HTTP-level API tests. Identity and Coordinator use different schemas in the
/// same database, so one container is enough for both factories.
/// </summary>
public sealed class ApiHostFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("api_host_tests")
                .WithUsername("msf")
                .WithPassword("msf")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"PostgreSQL Testcontainer unavailable: {exception.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            Assert.Skip(SkipReason ?? "PostgreSQL Testcontainer unavailable.");
        }
    }
}

[CollectionDefinition(nameof(ApiHostCollection))]
public sealed class ApiHostCollection : ICollectionFixture<ApiHostFixture>;
