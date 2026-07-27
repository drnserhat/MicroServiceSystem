using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
namespace MicroServiceSystem.Services.Location.Application.Abstractions;
public interface ICountryRepository : IRepository<Country, Guid>
{
    Task<IReadOnlyList<Country>> ListAllAsync(CancellationToken cancellationToken = default);
}
