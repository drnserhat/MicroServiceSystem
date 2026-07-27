using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Location.Application.Abstractions;

public interface ICountryRepository : IRepository<Country, Guid>
{
    Task<Country?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PagedResult<Country>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    uint GetConcurrencyVersion(Country country);

    void SetExpectedConcurrencyVersion(Country country, uint expectedVersion);
}
