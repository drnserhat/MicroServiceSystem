using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.ListMsfEntity;

public sealed record ListMsfEntityQuery(PaginationRequest Pagination) : IQuery<PagedResult<MsfEntityResponse>>;
