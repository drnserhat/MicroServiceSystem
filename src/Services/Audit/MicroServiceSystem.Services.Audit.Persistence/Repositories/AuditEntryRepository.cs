using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
namespace MicroServiceSystem.Services.Audit.Persistence.Repositories;
public sealed class AuditEntryRepository(AuditDbContext context) : EfRepository<AuditEntry, Guid>(context), IAuditEntryRepository
{
    public async Task<IReadOnlyList<AuditEntry>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);
}
