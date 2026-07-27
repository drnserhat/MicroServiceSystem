using MediatR;
using Microsoft.Extensions.Logging;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        TResponse response = await next();

        if (response is Result { IsFailure: true } failedResult)
        {
            logger.LogWarning(
                "Handled {RequestName} with failure {ErrorCode}: {ErrorDescription}",
                requestName,
                failedResult.Error.Code,
                failedResult.Error.Description);

            return response;
        }

        logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
