using MicroServiceSystem.SharedKernel.Abstractions;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;

public interface IMsfEntityRepository : IRepository<MsfEntityAggregate, Guid>
{
    Task<bool> NameExistsAsync(string name, Guid? excludedId = null, CancellationToken cancellationToken = default);
}
