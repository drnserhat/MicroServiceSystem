namespace MicroServiceSystem.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// Opt in contract for queries served from cache by the caching pipeline behavior.
/// </summary>
public interface ICacheableQuery
{
    string CacheCategory { get; }

    string CacheKeySuffix { get; }

    TimeSpan? AbsoluteExpiration => null;

    IReadOnlyCollection<string> CacheTags => [];
}
