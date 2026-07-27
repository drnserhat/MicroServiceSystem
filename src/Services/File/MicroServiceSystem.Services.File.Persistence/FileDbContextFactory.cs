using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.File.Persistence;

public sealed class FileDbContextFactory : DesignTimeDbContextFactoryBase<FileDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=file;Username=msf;Password=msf";

    protected override FileDbContext CreateNewInstance(
        DbContextOptions<FileDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
