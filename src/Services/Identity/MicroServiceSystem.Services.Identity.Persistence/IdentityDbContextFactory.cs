using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.Identity.Persistence;

public sealed class IdentityDbContextFactory : DesignTimeDbContextFactoryBase<IdentityDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=identity;Username=msf;Password=msf";

    protected override IdentityDbContext CreateNewInstance(
        DbContextOptions<IdentityDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
