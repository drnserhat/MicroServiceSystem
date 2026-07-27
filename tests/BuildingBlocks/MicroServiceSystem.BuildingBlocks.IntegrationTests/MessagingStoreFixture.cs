using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Inbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Outbox;
using MicroServiceSystem.SharedKernel.Abstractions;
using Testcontainers.PostgreSql;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class MessagingStoreFixture : IAsyncLifetime
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
                .WithDatabase("messaging_tests")
                .WithUsername("msf")
                .WithPassword("msf")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            await using MessagingTestDbContext context = CreateContext();
            await context.Database.EnsureCreatedAsync();

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

    /// <summary>
    /// Mirrors how services configure Npgsql in production. Retry on failure installs a retrying
    /// execution strategy, which rejects user-initiated transactions, so leaving it off here would hide
    /// exactly the kind of failure the messaging store has to survive.
    /// </summary>
    public MessagingTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null))
            .Options;

        return new MessagingTestDbContext(options);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureAvailable();

        await using MessagingTestDbContext context = CreateContext();
        await context.Set<OutboxMessage>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<InboxMessage>().ExecuteDeleteAsync(cancellationToken);
    }

    public void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            Assert.Skip(SkipReason ?? "PostgreSQL Testcontainer unavailable.");
        }
    }
}

public sealed class MessagingTestDbContext(DbContextOptions<MessagingTestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
    }
}

public sealed class MutableDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

[CollectionDefinition(nameof(MessagingStoreCollection))]
public sealed class MessagingStoreCollection : ICollectionFixture<MessagingStoreFixture>;
