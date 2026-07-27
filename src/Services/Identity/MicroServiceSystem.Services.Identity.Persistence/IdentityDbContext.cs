using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, DbContextDependencies dependencies)
    : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "identity";

    public DbSet<IdentityUser> Users => Set<IdentityUser>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override string Schema => DefaultSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
