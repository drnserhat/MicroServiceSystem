using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.User.Domain.Aggregates;

namespace MicroServiceSystem.Services.User.Persistence;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options, DbContextDependencies dependencies)
    : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "user";

    public DbSet<UserProfile> Profiles => Set<UserProfile>();

    protected override string Schema => DefaultSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
