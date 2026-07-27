using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Settings.Application.Abstractions;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Settings.Persistence.Repositories;

public sealed class SettingRepository(SettingsDbContext context)
    : EfRepository<Setting, Guid>(context), ISettingRepository
{
    public Task<Setting?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(setting => setting.Key == key.Trim(), cancellationToken);

    public async Task<PagedResult<Setting>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Setting> query = Set;
        long totalCount = await query.LongCountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<Setting>.Empty(pagination);
        }

        List<Setting> items = await query
            .OrderBy(setting => setting.Key)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<Setting>.Create(items, totalCount, pagination);
    }

    public uint GetConcurrencyVersion(Setting setting) =>
        OptimisticConcurrency.GetVersion(Context, setting);

    public void SetExpectedConcurrencyVersion(Setting setting, uint expectedVersion) =>
        OptimisticConcurrency.SetExpectedVersion(Context, setting, expectedVersion);
}
