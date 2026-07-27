using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Saga;

/// <summary>
/// A single compensating step in an orchestrated saga.
/// Steps return <see cref="Result"/> so handlers stay aligned with the framework Result pattern.
/// </summary>
public interface ISagaStep<in TContext>
{
    string Name { get; }

    Task<Result> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task<Result> CompensateAsync(TContext context, CancellationToken cancellationToken = default);
}
