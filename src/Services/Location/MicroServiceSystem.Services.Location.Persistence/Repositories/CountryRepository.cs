using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
namespace MicroServiceSystem.Services.Location.Persistence.Repositories;
public sealed class CountryRepository(LocationDbContext context) : EfRepository<Country, Guid>(context), ICountryRepository
{
    public async Task<IReadOnlyList<Country>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);
}
