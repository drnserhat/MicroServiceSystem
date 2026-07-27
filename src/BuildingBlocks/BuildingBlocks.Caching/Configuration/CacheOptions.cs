namespace MicroServiceSystem.BuildingBlocks.Caching.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public CacheProvider Provider { get; set; } = CacheProvider.Hybrid;

    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = string.Empty;

    public int DefaultAbsoluteExpirationMinutes { get; set; } = 10;

    public int DefaultSlidingExpirationMinutes { get; set; } = 2;

    public int LocalCacheSizeLimitMegabytes { get; set; } = 64;

    public bool EnableCompression { get; set; } = true;
}

public enum CacheProvider
{
    Memory = 0,
    Redis = 1,
    Hybrid = 2
}
