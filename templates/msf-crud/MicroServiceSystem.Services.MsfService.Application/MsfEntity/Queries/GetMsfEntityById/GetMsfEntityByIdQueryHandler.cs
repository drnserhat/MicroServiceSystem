using MapsterMapper;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;
using MicroServiceSystem.SharedKernel.Results;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.GetMsfEntityById;

internal sealed class GetMsfEntityByIdQueryHandler(IMsfEntityRepository repository, IMapper mapper)
    : IQueryHandler<GetMsfEntityByIdQuery, MsfEntityResponse>
{
    public async Task<Result<MsfEntityResponse>> Handle(
        GetMsfEntityByIdQuery query,
        CancellationToken cancellationToken)
    {
        MsfEntityAggregate? entity = await repository.GetByIdAsync(query.Id, cancellationToken);

        return entity is null
            ? Result.Failure<MsfEntityResponse>(MsfEntityErrors.NotFound(query.Id))
            : Result.Success(mapper.Map<MsfEntityResponse>(entity));
    }
}
