using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Saga;

/// <summary>
/// Runs saga steps in order and compensates previously completed steps in reverse on failure.
/// </summary>
public static class SagaRunner
{
    public static async Task<Result> RunAsync<TContext>(
        IReadOnlyList<ISagaStep<TContext>> steps,
        TContext context,
        CancellationToken cancellationToken = default)
    {
        Ensure.NotNull(steps);

        if (steps.Count == 0)
        {
            return Result.Success();
        }

        var executed = new Stack<ISagaStep<TContext>>();

        foreach (ISagaStep<TContext> step in steps)
        {
            Ensure.NotNull(step);

            Result executionResult = await ExecuteStepAsync(step, context, cancellationToken);

            if (executionResult.IsFailure)
            {
                Result compensationResult = await CompensateAsync(executed, context, cancellationToken);
                return compensationResult.IsFailure ? compensationResult : executionResult;
            }

            executed.Push(step);
        }

        return Result.Success();
    }

    private static async Task<Result> ExecuteStepAsync<TContext>(
        ISagaStep<TContext> step,
        TContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await step.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure(
                Error.Failure(
                    SagaErrorCodes.StepFaulted,
                    $"Saga step '{step.Name}' faulted: {ex.Message}"));
        }
    }

    private static async Task<Result> CompensateAsync<TContext>(
        Stack<ISagaStep<TContext>> executed,
        TContext context,
        CancellationToken cancellationToken)
    {
        while (executed.Count > 0)
        {
            ISagaStep<TContext> step = executed.Pop();

            Result compensationResult;
            try
            {
                compensationResult = await step.CompensateAsync(context, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result.Failure(
                    Error.Failure(
                        SagaErrorCodes.CompensationFailed,
                        $"Saga step '{step.Name}' compensation faulted: {ex.Message}"));
            }

            if (compensationResult.IsFailure)
            {
                return Result.Failure(
                    Error.Failure(
                        SagaErrorCodes.CompensationFailed,
                        $"Saga step '{step.Name}' compensation failed: {compensationResult.Error.Description}"));
            }
        }

        return Result.Success();
    }
}
