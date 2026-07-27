namespace MicroServiceSystem.BuildingBlocks.Saga;

public static class SagaOwner
{
    /// <summary>
    /// Identifies this process when claiming a saga. It only has to be stable for the lifetime of the
    /// process and distinct between replicas; it is never used for authorization.
    /// </summary>
    public static string Current { get; } = $"{Environment.MachineName}:{Environment.ProcessId}";
}
