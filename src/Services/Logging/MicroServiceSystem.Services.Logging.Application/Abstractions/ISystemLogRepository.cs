using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Logging.Application.Abstractions;

public interface ISystemLogRepository
{
    Task AddAsync(SystemLogDocument document, CancellationToken cancellationToken = default);

    Task<SystemLogDocument?> FindByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SystemLogDocument>> PagedListAsync(
        Guid tenantId,
        SystemLogListFilter filter,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}

public sealed record SystemLogListFilter(
    string? Level = null,
    string? Source = null,
    string? CorrelationId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);

public sealed class SystemLogDocument
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Source { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset Timestamp { get; init; }
}
