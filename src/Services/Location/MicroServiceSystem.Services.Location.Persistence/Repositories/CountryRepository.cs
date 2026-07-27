using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Location.Persistence.Repositories;

public sealed class CountryRepository(LocationDbContext context)
    : EfRepository<Country, Guid>(context), ICountryRepository
{
    public Task<Country?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(country => country.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public async Task<PagedResult<Country>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        // Tracked so xmin Version is available for list ETag responses.
        IQueryable<Country> query = Set;
        long totalCount = await query.LongCountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<Country>.Empty(pagination);
        }

        List<Country> items = await query
            .OrderBy(country => country.Code)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<Country>.Create(items, totalCount, pagination);
    }

    public uint GetConcurrencyVersion(Country country) =>
        OptimisticConcurrency.GetVersion(Context, country);

    public void SetExpectedConcurrencyVersion(Country country, uint expectedVersion) =>
        OptimisticConcurrency.SetExpectedVersion(Context, country, expectedVersion);
}
