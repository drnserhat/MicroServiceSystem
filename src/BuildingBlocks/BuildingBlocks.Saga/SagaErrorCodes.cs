namespace MicroServiceSystem.BuildingBlocks.Saga;

public static class SagaErrorCodes
{
    public const string StepFaulted = "saga.step_faulted";

    public const string CompensationFailed = "saga.compensation_failed";
}
