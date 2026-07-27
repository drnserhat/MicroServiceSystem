using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Audit.Persistence.Repositories;

public sealed class AuditEntryRepository(AuditDbContext context)
    : EfRepository<AuditEntry, Guid>(context), IAuditEntryRepository
{
    public async Task<PagedResult<AuditEntry>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<AuditEntry> query = Set.AsNoTracking();
        long totalCount = await query.LongCountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<AuditEntry>.Empty(pagination);
        }

        List<AuditEntry> items = await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<AuditEntry>.Create(items, totalCount, pagination);
    }
}
