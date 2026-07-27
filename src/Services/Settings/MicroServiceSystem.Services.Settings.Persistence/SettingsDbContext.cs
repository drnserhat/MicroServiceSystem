using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;
namespace MicroServiceSystem.Services.Settings.Persistence;
public sealed class SettingsDbContext(DbContextOptions<SettingsDbContext> options, DbContextDependencies dependencies) : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "settings";
    public DbSet<Setting> Settings => Set<Setting>();
    protected override string Schema => DefaultSchema;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SettingsDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
