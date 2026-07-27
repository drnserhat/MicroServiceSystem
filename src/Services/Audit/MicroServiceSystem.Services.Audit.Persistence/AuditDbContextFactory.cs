using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.Audit.Persistence;

public sealed class AuditDbContextFactory : DesignTimeDbContextFactoryBase<AuditDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=audit;Username=msf;Password=msf";

    protected override AuditDbContext CreateNewInstance(
        DbContextOptions<AuditDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
