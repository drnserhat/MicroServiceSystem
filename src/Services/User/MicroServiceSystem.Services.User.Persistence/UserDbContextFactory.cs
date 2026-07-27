using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace MicroServiceSystem.Services.User.Persistence;

public sealed class UserDbContextFactory : DesignTimeDbContextFactoryBase<UserDbContext>
{
    protected override string DefaultConnectionString =>
        "Host=localhost;Port=5432;Database=user;Username=msf;Password=msf";

    protected override UserDbContext CreateNewInstance(
        DbContextOptions<UserDbContext> options,
        DbContextDependencies dependencies) =>
        new(options, dependencies);
}
