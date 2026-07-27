using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.Location.Persistence;

public sealed class LocationDbContextFactory : DesignTimeDbContextFactoryBase<LocationDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=location;Username=msf;Password=msf";

    protected override LocationDbContext CreateNewInstance(
        DbContextOptions<LocationDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
