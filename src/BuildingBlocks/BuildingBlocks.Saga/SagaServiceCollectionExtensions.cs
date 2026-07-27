using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MicroServiceSystem.BuildingBlocks.Saga;

public static class SagaServiceCollectionExtensions
{
    public static IServiceCollection AddFrameworkSaga(this IServiceCollection services)
    {
        services.TryAddScoped<ISagaCheckpoint, UnitOfWorkSagaCheckpoint>();
        return services;
    }
}
