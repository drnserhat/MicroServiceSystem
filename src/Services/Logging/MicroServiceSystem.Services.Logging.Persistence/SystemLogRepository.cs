using MongoDB.Driver;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.Services.Logging.Application.Abstractions;

namespace MicroServiceSystem.Services.Logging.Persistence;

public sealed class SystemLogRepository(IMongoContext context) : ISystemLogRepository
{
    private IMongoCollection<SystemLogDocument> Collection =>
        context.Collection<SystemLogDocument>("system_logs");

    public Task AddAsync(SystemLogDocument document, CancellationToken cancellationToken = default) =>
        Collection.InsertOneAsync(document, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<SystemLogDocument>> ListAsync(
        Guid tenantId,
        string? level = null,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        FilterDefinition<SystemLogDocument> filter = Builders<SystemLogDocument>.Filter.Eq(x => x.TenantId, tenantId);

        if (!string.IsNullOrWhiteSpace(level))
        {
            filter &= Builders<SystemLogDocument>.Filter.Eq(x => x.Level, level);
        }

        return await Collection
            .Find(filter)
            .SortByDescending(document => document.Timestamp)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }
}
