using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Coordinator.Domain.Aggregates;

namespace Coordinator.Persistence;

public sealed class CoordinatorDbContext(DbContextOptions<CoordinatorDbContext> options, DbContextDependencies dependencies)
    : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "coordinator";

    public DbSet<RegisterUserSaga> RegisterUserSagas => Set<RegisterUserSaga>();

    protected override string Schema => DefaultSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoordinatorDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
