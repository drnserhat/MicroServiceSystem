using MicroServiceSystem.Services.User.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.Services.User.Application.Abstractions;

public interface IUserProfileRepository : IRepository<UserProfile, Guid>
{
    uint GetConcurrencyVersion(UserProfile profile);

    void SetExpectedConcurrencyVersion(UserProfile profile, uint expectedVersion);
}
