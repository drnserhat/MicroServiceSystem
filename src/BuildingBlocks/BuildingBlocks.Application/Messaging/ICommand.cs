using MediatR;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Messaging;

public interface ICommandBase;

public interface ICommand : IRequest<Result>, ICommandBase;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, ICommandBase;
