using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.File.Domain.Aggregates;
namespace MicroServiceSystem.Services.File.Persistence;
public sealed class FileDbContext(DbContextOptions<FileDbContext> options, DbContextDependencies dependencies) : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "file";
    public DbSet<FileAsset> Files => Set<FileAsset>();
    protected override string Schema => DefaultSchema;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
