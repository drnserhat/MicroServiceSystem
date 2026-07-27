using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MicroServiceSystem.BuildingBlocks.Persistence.Extensions;
using MicroServiceSystem.Services.Logging.Application.Abstractions;

namespace MicroServiceSystem.Services.Logging.Persistence;

public static class LoggingPersistenceExtensions
{
    public static IServiceCollection AddLoggingPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMongoPersistence(configuration);
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();
        services.AddHostedService<SystemLogIndexInitializer>();
        return services;
    }
}

internal sealed class SystemLogIndexInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ISystemLogRepository logs = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
        await logs.EnsureIndexesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
