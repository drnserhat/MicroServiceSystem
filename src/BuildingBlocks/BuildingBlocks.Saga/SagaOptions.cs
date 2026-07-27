namespace MicroServiceSystem.BuildingBlocks.Saga;

/// <summary>
/// Recovery polling for durable orchestration sagas.
/// </summary>
public sealed class SagaOptions
{
    public const string SectionName = "Saga";

    public bool RecoveryEnabled { get; set; } = true;

    /// <summary>How often the recovery worker scans for abandoned sagas.</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// How long an owner holds a saga before recovery may take it over. It must comfortably exceed the
    /// time between two checkpoints, otherwise recovery starts compensating sagas that are merely slow.
    /// </summary>
    public int LeaseSeconds { get; set; } = 120;

    public int BatchSize { get; set; } = 25;
}
