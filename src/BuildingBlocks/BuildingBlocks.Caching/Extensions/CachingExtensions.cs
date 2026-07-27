using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Caching.Abstractions;
using MicroServiceSystem.BuildingBlocks.Caching.Behaviors;
using MicroServiceSystem.BuildingBlocks.Caching.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Caching.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddFrameworkCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName))
            .ValidateOnStart();

        CacheOptions cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        if (cacheOptions.Provider is CacheProvider.Redis or CacheProvider.Hybrid
            && !string.IsNullOrWhiteSpace(cacheOptions.ConnectionString))
        {
            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = cacheOptions.ConnectionString;
                redisOptions.InstanceName = string.IsNullOrWhiteSpace(cacheOptions.InstanceName)
                    ? null
                    : $"{cacheOptions.InstanceName}:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHybridCache(hybridOptions =>
        {
            hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheOptions.DefaultAbsoluteExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheOptions.DefaultSlidingExpirationMinutes)
            };
        });

        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }

    /// <summary>
    /// Adds the query caching behavior. Registered after the application building block so it runs
    /// closer to the handler than logging and validation.
    /// </summary>
    public static IServiceCollection AddQueryCaching(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));

        return services;
    }
}
