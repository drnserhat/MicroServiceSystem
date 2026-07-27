using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.User.Application.Abstractions;
using MicroServiceSystem.Services.User.Domain.Aggregates;

namespace MicroServiceSystem.Services.User.Persistence.Repositories;

public sealed class UserProfileRepository(UserDbContext context)
    : EfRepository<UserProfile, Guid>(context), IUserProfileRepository;
