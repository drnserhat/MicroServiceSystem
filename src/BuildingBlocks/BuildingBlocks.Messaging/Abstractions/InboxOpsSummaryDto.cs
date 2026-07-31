namespace MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;

/// <summary>Shared ops response for per-service inbox summary (counts only — no message keys).</summary>
public sealed record InboxOpsSummaryDto(
    string Service,
    int ProcessedCount,
    int OpenCount,
    int InFlightCount,
    int FailedCount);
