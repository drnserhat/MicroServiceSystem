using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Caching.Abstractions;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.GetMsfEntityById;

public sealed record GetMsfEntityByIdQuery(Guid Id) : IQuery<MsfEntityResponse>, ICacheableQuery
{
    public string CacheCategory => "msfentity";

    public string CacheKeySuffix => Id.ToString();

    public IReadOnlyCollection<string> CacheTags => ["msfentity"];
}
