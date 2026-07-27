using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Saga;

/// <summary>
/// Persists saga progress through the ambient unit of work (typically EF SaveChanges).
/// </summary>
public sealed class UnitOfWorkSagaCheckpoint(IUnitOfWork unitOfWork) : ISagaCheckpoint
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        unitOfWork.SaveChangesAsync(cancellationToken);
}
