using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.CreateMsfEntity;

internal sealed class CreateMsfEntityCommandHandler(IMsfEntityRepository repository)
    : ICommandHandler<CreateMsfEntityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMsfEntityCommand command, CancellationToken cancellationToken)
    {
        if (await repository.NameExistsAsync(command.Name, cancellationToken: cancellationToken))
        {
            return Result.Failure<Guid>(MsfEntityErrors.NameAlreadyExists(command.Name));
        }

        MsfEntityAggregate entity = MsfEntityAggregate.Create(command.Name, command.Description);

        await repository.AddAsync(entity, cancellationToken);

        return Result.Success(entity.Id);
    }
}
