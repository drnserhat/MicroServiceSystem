using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.Notification.Persistence;

public sealed class NotificationDbContextFactory : DesignTimeDbContextFactoryBase<NotificationDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=notification;Username=msf;Password=msf";

    protected override NotificationDbContext CreateNewInstance(
        DbContextOptions<NotificationDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
