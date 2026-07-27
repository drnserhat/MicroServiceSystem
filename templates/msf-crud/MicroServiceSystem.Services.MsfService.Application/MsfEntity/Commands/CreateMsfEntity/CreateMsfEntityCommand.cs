using MicroServiceSystem.BuildingBlocks.Application.Messaging;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.CreateMsfEntity;

public sealed record CreateMsfEntityCommand(string Name, string? Description) : ICommand<Guid>;
