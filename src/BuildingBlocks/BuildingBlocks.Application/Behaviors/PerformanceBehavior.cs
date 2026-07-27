using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Application.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    IOptions<ApplicationPipelineOptions> options) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        long startingTimestamp = Stopwatch.GetTimestamp();

        TResponse response = await next();

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startingTimestamp);

        if (elapsed.TotalMilliseconds >= options.Value.SlowRequestThresholdMilliseconds)
        {
            logger.LogWarning(
                "Long running request {RequestName} completed in {ElapsedMilliseconds} ms",
                typeof(TRequest).Name,
                elapsed.TotalMilliseconds);
        }

        return response;
    }
}
