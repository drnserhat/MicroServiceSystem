using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.Settings.Persistence;

public sealed class SettingsDbContextFactory : DesignTimeDbContextFactoryBase<SettingsDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=settings;Username=msf;Password=msf";

    protected override SettingsDbContext CreateNewInstance(
        DbContextOptions<SettingsDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
