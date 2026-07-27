namespace MicroServiceSystem.BuildingBlocks.Caching.Abstractions;

public sealed record CacheEntryOptions
{
    public TimeSpan? AbsoluteExpiration { get; init; }

    public TimeSpan? SlidingExpiration { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = [];

    public static CacheEntryOptions Absolute(TimeSpan expiration) => new() { AbsoluteExpiration = expiration };

    public static CacheEntryOptions Sliding(TimeSpan expiration) => new() { SlidingExpiration = expiration };

    public CacheEntryOptions WithTags(params string[] tags) => this with { Tags = tags };
}
