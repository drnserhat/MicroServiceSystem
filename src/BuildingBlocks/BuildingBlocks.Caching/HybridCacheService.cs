using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Caching.Abstractions;
using MicroServiceSystem.BuildingBlocks.Caching.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Caching;

public sealed class HybridCacheService(HybridCache cache, IOptions<CacheOptions> options) : ICacheService
{
    private static readonly HybridCacheEntryOptions ReadOnlyEntryOptions = new()
    {
        Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        bool found = true;

        T? value = await cache.GetOrCreateAsync(
            key,
            _ =>
            {
                found = false;
                return ValueTask.FromResult<T?>(default);
            },
            ReadOnlyEntryOptions,
            cancellationToken: cancellationToken);

        return found ? value : default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? entryOptions = null,
        CancellationToken cancellationToken = default) =>
        await cache.SetAsync(key, value, ToHybridOptions(entryOptions), entryOptions?.Tags, cancellationToken);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? entryOptions = null,
        CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync(
            key,
            factory,
            static async (state, token) => await state(token),
            ToHybridOptions(entryOptions),
            entryOptions?.Tags,
            cancellationToken);

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(key, cancellationToken);

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        await cache.RemoveByTagAsync(tag, cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        bool found = true;

        await cache.GetOrCreateAsync(
            key,
            _ =>
            {
                found = false;
                return ValueTask.FromResult<string?>(null);
            },
            ReadOnlyEntryOptions,
            cancellationToken: cancellationToken);

        return found;
    }

    private HybridCacheEntryOptions ToHybridOptions(CacheEntryOptions? entryOptions)
    {
        CacheOptions cacheOptions = options.Value;

        TimeSpan absolute = entryOptions?.AbsoluteExpiration
            ?? TimeSpan.FromMinutes(cacheOptions.DefaultAbsoluteExpirationMinutes);

        TimeSpan local = entryOptions?.SlidingExpiration
            ?? TimeSpan.FromMinutes(cacheOptions.DefaultSlidingExpirationMinutes);

        return new HybridCacheEntryOptions
        {
            Expiration = absolute,
            LocalCacheExpiration = local < absolute ? local : absolute
        };
    }
}
