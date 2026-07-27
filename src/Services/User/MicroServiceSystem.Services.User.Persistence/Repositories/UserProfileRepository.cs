using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;

namespace MicroServiceSystem.Services.User.Persistence.Repositories;

public sealed class UserProfileRepository(UserDbContext context)
    : EfRepository<UserProfile, Guid>(context), IUserProfileRepository
{
    public uint GetConcurrencyVersion(UserProfile profile) =>
        OptimisticConcurrency.GetVersion(Context, profile);

    public void SetExpectedConcurrencyVersion(UserProfile profile, uint expectedVersion) =>
        OptimisticConcurrency.SetExpectedVersion(Context, profile, expectedVersion);
}
