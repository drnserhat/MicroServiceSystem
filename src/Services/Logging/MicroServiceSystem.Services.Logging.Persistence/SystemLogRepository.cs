using MongoDB.Driver;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.Services.Logging.Application.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Logging.Persistence;

public sealed class SystemLogRepository(IMongoContext context) : ISystemLogRepository
{
    private IMongoCollection<SystemLogDocument> Collection =>
        context.Collection<SystemLogDocument>("system_logs");

    public Task AddAsync(SystemLogDocument document, CancellationToken cancellationToken = default) =>
        Collection.InsertOneAsync(document, cancellationToken: cancellationToken);

    public async Task<SystemLogDocument?> FindByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        FilterDefinition<SystemLogDocument> filter =
            Builders<SystemLogDocument>.Filter.Eq(document => document.TenantId, tenantId)
            & Builders<SystemLogDocument>.Filter.Eq(document => document.Id, id);

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<SystemLogDocument>> PagedListAsync(
        Guid tenantId,
        SystemLogListFilter filter,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(pagination);

        FilterDefinitionBuilder<SystemLogDocument> builder = Builders<SystemLogDocument>.Filter;
        FilterDefinition<SystemLogDocument> mongoFilter = builder.Eq(document => document.TenantId, tenantId);

        if (!string.IsNullOrWhiteSpace(filter.Level))
        {
            mongoFilter &= builder.Eq(document => document.Level, filter.Level.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            mongoFilter &= builder.Eq(document => document.Source, filter.Source.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            mongoFilter &= builder.Eq(document => document.CorrelationId, filter.CorrelationId.Trim());
        }

        if (filter.FromUtc is DateTimeOffset fromUtc)
        {
            mongoFilter &= builder.Gte(document => document.Timestamp, fromUtc);
        }

        if (filter.ToUtc is DateTimeOffset toUtc)
        {
            mongoFilter &= builder.Lte(document => document.Timestamp, toUtc);
        }

        long totalCount = await Collection.CountDocumentsAsync(mongoFilter, cancellationToken: cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<SystemLogDocument>.Empty(pagination);
        }

        List<SystemLogDocument> items = await Collection
            .Find(mongoFilter)
            .SortByDescending(document => document.Timestamp)
            .Skip(pagination.Skip)
            .Limit(pagination.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<SystemLogDocument>.Create(items, totalCount, pagination);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        IndexKeysDefinitionBuilder<SystemLogDocument> keys = Builders<SystemLogDocument>.IndexKeys;

        CreateIndexModel<SystemLogDocument>[] models =
        [
            new(keys.Ascending(document => document.TenantId).Descending(document => document.Timestamp)),
            new(keys.Ascending(document => document.TenantId)
                .Ascending(document => document.Level)
                .Descending(document => document.Timestamp)),
            new(keys.Ascending(document => document.TenantId)
                .Ascending(document => document.CorrelationId)
                .Descending(document => document.Timestamp))
        ];

        await Collection.Indexes.CreateManyAsync(models, cancellationToken);
    }
}
