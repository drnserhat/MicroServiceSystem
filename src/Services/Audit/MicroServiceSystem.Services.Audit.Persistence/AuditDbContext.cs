using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
namespace MicroServiceSystem.Services.Audit.Persistence;
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options, DbContextDependencies dependencies) : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "audit";
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    protected override string Schema => DefaultSchema;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
