using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;

namespace Coordinator.Persistence.Repositories;

public sealed class RegisterUserSagaRepository(CoordinatorDbContext context)
    : EfRepository<RegisterUserSaga, Guid>(context), IRegisterUserSagaRepository;
