using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.UpdateMsfEntity;

internal sealed class UpdateMsfEntityCommandHandler(IMsfEntityRepository repository)
    : ICommandHandler<UpdateMsfEntityCommand>
{
    public async Task<Result> Handle(UpdateMsfEntityCommand command, CancellationToken cancellationToken)
    {
        MsfEntityAggregate? entity = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(MsfEntityErrors.NotFound(command.Id));
        }

        if (await repository.NameExistsAsync(command.Name, command.Id, cancellationToken))
        {
            return Result.Failure(MsfEntityErrors.NameAlreadyExists(command.Name));
        }

        entity.Rename(command.Name);
        entity.ChangeDescription(command.Description);

        repository.Update(entity);

        return Result.Success();
    }
}
