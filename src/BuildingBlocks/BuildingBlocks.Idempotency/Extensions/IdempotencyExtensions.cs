using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Idempotency.Abstractions;
using MicroServiceSystem.BuildingBlocks.Idempotency.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Idempotency.Extensions;

public static class IdempotencyExtensions
{
    public static IServiceCollection AddFrameworkIdempotency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IIdempotencyStore, DistributedCacheIdempotencyStore>();

        return services;
    }

    public static IApplicationBuilder UseFrameworkIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
