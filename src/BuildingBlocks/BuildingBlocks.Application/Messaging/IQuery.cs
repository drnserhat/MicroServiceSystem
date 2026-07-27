using MediatR;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Messaging;

public interface IQueryBase;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>, IQueryBase;
