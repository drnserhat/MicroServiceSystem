using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Persistence.Repositories;

internal sealed class MsfEntityRepository(MsfServiceDbContext dbContext)
    : EfRepository<MsfEntityAggregate, Guid>(dbContext), IMsfEntityRepository
{
    public Task<bool> NameExistsAsync(string name, Guid? excludedId = null, CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            entity => entity.Name == name && (excludedId == null || entity.Id != excludedId),
            cancellationToken);
}
