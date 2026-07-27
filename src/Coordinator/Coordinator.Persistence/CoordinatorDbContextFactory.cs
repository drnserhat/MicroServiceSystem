using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Coordinator.Persistence;

public sealed class CoordinatorDbContextFactory : DesignTimeDbContextFactoryBase<CoordinatorDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=coordinator;Username=msf;Password=msf";

    protected override CoordinatorDbContext CreateNewInstance(
        DbContextOptions<CoordinatorDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
