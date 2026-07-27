namespace MicroServiceSystem.Services.Logging.Application.Abstractions;

public interface ISystemLogRepository
{
    Task AddAsync(SystemLogDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemLogDocument>> ListAsync(
        Guid tenantId,
        string? level = null,
        int take = 100,
        CancellationToken cancellationToken = default);
}

public sealed class SystemLogDocument
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Source { get; init; }

    public DateTimeOffset Timestamp { get; init; }
}
