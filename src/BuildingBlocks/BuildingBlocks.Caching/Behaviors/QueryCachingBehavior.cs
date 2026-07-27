using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using MicroServiceSystem.BuildingBlocks.Caching.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Caching.Behaviors;

/// <summary>
/// Serves queries that opt in through <see cref="ICacheableQuery"/> from cache. Only successful
/// results are cached, so a transient failure never poisons the cache.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ICacheKeyBuilder cacheKeyBuilder,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, ICacheProxy> Proxies = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable || !TryGetResultValueType(out Type? valueType))
        {
            return await next();
        }

        string key = cacheKeyBuilder.Build(cacheable.CacheCategory, cacheable.CacheKeySuffix);

        var entryOptions = new CacheEntryOptions
        {
            AbsoluteExpiration = cacheable.AbsoluteExpiration,
            Tags = cacheable.CacheTags
        };

        ICacheProxy proxy = Proxies.GetOrAdd(
            valueType!,
            static type => (ICacheProxy)Activator.CreateInstance(typeof(CacheProxy<>).MakeGenericType(type))!);

        object response = await proxy.ExecuteAsync(
            cacheService,
            key,
            entryOptions,
            async () => (await next())!,
            logger,
            cancellationToken);

        return (TResponse)response;
    }

    private static bool TryGetResultValueType(out Type? valueType)
    {
        Type responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            valueType = responseType.GetGenericArguments()[0];
            return true;
        }

        valueType = null;
        return false;
    }

    private interface ICacheProxy
    {
        Task<object> ExecuteAsync(
            ICacheService cacheService,
            string key,
            CacheEntryOptions entryOptions,
            Func<Task<object>> next,
            ILogger logger,
            CancellationToken cancellationToken);
    }

    private sealed class CacheProxy<TValue> : ICacheProxy
    {
        public async Task<object> ExecuteAsync(
            ICacheService cacheService,
            string key,
            CacheEntryOptions entryOptions,
            Func<Task<object>> next,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            TValue? cached = await cacheService.GetAsync<TValue>(key, cancellationToken);

            if (cached is not null)
            {
                logger.LogDebug("Cache hit for {CacheKey}", key);
                return Result.Success(cached);
            }

            object response = await next();

            if (response is Result<TValue> { IsSuccess: true } success)
            {
                await cacheService.SetAsync(key, success.Value, entryOptions, cancellationToken);
            }

            return response;
        }
    }
}
