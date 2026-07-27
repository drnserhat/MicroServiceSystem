using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.DeleteMsfEntity;

internal sealed class DeleteMsfEntityCommandHandler(IMsfEntityRepository repository)
    : ICommandHandler<DeleteMsfEntityCommand>
{
    public async Task<Result> Handle(DeleteMsfEntityCommand command, CancellationToken cancellationToken)
    {
        MsfEntityAggregate? entity = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(MsfEntityErrors.NotFound(command.Id));
        }

        repository.Remove(entity);

        return Result.Success();
    }
}
