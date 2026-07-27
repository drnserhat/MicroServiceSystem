namespace MicroServiceSystem.BuildingBlocks.Idempotency.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> TryReserveAsync(string key, TimeSpan retention, CancellationToken cancellationToken = default);

    Task<string?> GetResponseAsync(string key, CancellationToken cancellationToken = default);

    Task StoreResponseAsync(
        string key,
        string response,
        TimeSpan retention,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(string key, CancellationToken cancellationToken = default);
}
