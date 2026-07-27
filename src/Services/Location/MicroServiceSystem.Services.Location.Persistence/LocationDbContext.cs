using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
namespace MicroServiceSystem.Services.Location.Persistence;
public sealed class LocationDbContext(DbContextOptions<LocationDbContext> options, DbContextDependencies dependencies) : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "location";
    public DbSet<Country> Countries => Set<Country>();
    protected override string Schema => DefaultSchema;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LocationDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
