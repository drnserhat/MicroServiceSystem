using MicroServiceSystem.Services.Settings.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Settings.Application.Abstractions;

public interface ISettingRepository : IRepository<Setting, Guid>
{
    Task<Setting?> FindByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<PagedResult<Setting>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    uint GetConcurrencyVersion(Setting setting);

    void SetExpectedConcurrencyVersion(Setting setting, uint expectedVersion);
}
