using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Audit.Application.Abstractions;

public interface IAuditEntryRepository : IRepository<AuditEntry, Guid>
{
    Task<PagedResult<AuditEntry>> PagedListAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);
}
