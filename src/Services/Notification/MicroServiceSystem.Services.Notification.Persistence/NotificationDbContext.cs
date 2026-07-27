using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Notification.Domain.Aggregates;
namespace MicroServiceSystem.Services.Notification.Persistence;
public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options, DbContextDependencies dependencies) : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "notification";
    public DbSet<NotificationMessage> Notifications => Set<NotificationMessage>();
    protected override string Schema => DefaultSchema;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
