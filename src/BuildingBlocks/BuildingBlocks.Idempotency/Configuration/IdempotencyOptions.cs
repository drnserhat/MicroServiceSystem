using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Idempotency.Configuration;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public bool Enabled { get; set; } = true;

    public string HeaderName { get; set; } = FrameworkHeaders.IdempotencyKey;

    public int RetentionHours { get; set; } = 24;

    public bool RequireKeyForMutations { get; set; }
}
