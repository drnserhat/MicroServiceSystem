using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
namespace MicroServiceSystem.Services.Audit.Application.Abstractions;
public interface IAuditEntryRepository : IRepository<AuditEntry, Guid>
{
    Task<IReadOnlyList<AuditEntry>> ListAllAsync(CancellationToken cancellationToken = default);
}
