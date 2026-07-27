using MapsterMapper;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Abstractions;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;
using MicroServiceSystem.Services.MsfService.Domain.Specifications;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.ListMsfEntity;

internal sealed class ListMsfEntityQueryHandler(IMsfEntityRepository repository, IMapper mapper)
    : IQueryHandler<ListMsfEntityQuery, PagedResult<MsfEntityResponse>>
{
    public async Task<Result<PagedResult<MsfEntityResponse>>> Handle(
        ListMsfEntityQuery query,
        CancellationToken cancellationToken)
    {
        var specification = new MsfEntitySearchSpecification(query.Pagination);

        PagedResult<MsfEntityAggregate> page =
            await repository.PagedListAsync(specification, query.Pagination, cancellationToken);

        return Result.Success(page.Project(mapper.Map<MsfEntityResponse>));
    }
}
