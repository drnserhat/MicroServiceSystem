using MicroServiceSystem.BuildingBlocks.Application.Messaging;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.DeleteMsfEntity;

public sealed record DeleteMsfEntityCommand(Guid Id) : ICommand;
