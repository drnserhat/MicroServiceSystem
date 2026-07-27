using Mapster;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Mappings;

public sealed class MsfEntityMappingRegistration : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<MsfEntityAggregate, MsfEntityResponse>();
}
