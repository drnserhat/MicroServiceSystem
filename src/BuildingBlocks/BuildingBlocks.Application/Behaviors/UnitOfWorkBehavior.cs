using MediatR;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Commits the unit of work once per command, after the handler succeeded. Handlers mutate
/// aggregates only; transaction boundaries are owned by the pipeline.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response = await next();

        if (response is Result { IsFailure: true })
        {
            return response;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
