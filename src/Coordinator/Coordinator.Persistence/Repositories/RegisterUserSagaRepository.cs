using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;

namespace Coordinator.Persistence.Repositories;

public sealed class RegisterUserSagaRepository(CoordinatorDbContext context)
    : EfRepository<RegisterUserSaga, Guid>(context), IRegisterUserSagaRepository
{
    public async Task<IReadOnlyList<RegisterUserSaga>> ListAbandonedAsync(
        DateTimeOffset utcNow,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        return await Set.AsQueryable()
            .Where(saga =>
                saga.State != RegisterUserSagaState.Completed
                && saga.State != RegisterUserSagaState.Failed)
            .Where(saga => saga.LockedUntilUtc == null || saga.LockedUntilUtc <= utcNow)
            .OrderBy(saga => saga.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
