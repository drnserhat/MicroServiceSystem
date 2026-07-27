using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Contexts;

namespace MicroServiceSystem.Services.MsfService.Persistence;

public sealed class MsfServiceDbContext(DbContextOptions<MsfServiceDbContext> options, DbContextDependencies dependencies)
    : FrameworkDbContext(options, dependencies)
{
    public const string DefaultSchema = "msfservice";

    protected override string Schema => DefaultSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MsfServiceDbContext).Assembly);
        modelBuilder.ApplyMessagingStore();
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
