using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Saga;

/// <summary>
/// Optional hook for durable orchestration: flush saga state to the store
/// before/after remote side effects so a crash can be recovered.
/// </summary>
public interface ISagaCheckpoint
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// No-op checkpoint for in-memory tests or non-durable runners.
/// </summary>
public sealed class NullSagaCheckpoint : ISagaCheckpoint
{
    public static NullSagaCheckpoint Instance { get; } = new();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
