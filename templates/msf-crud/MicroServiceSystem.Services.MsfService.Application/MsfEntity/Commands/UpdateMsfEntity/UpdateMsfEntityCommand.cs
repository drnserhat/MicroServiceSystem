using MicroServiceSystem.BuildingBlocks.Application.Messaging;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.UpdateMsfEntity;

public sealed record UpdateMsfEntityCommand(Guid Id, string Name, string? Description) : ICommand;
