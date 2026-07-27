using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Logging.Application.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Logging.Application;

public sealed record IngestSystemLogCommand(
    string Level,
    string Message,
    string? Source,
    DateTimeOffset Timestamp,
    Guid TenantId) : ICommand;

public sealed class IngestSystemLogCommandValidator : AbstractValidator<IngestSystemLogCommand>
{
    public IngestSystemLogCommandValidator()
    {
        RuleFor(command => command.Level).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Message).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class IngestSystemLogCommandHandler(
    ISystemLogRepository logs,
    ICurrentTenant currentTenant) : ICommandHandler<IngestSystemLogCommand>
{
    public async Task<Result> Handle(IngestSystemLogCommand command, CancellationToken cancellationToken)
    {
        using IDisposable scope = currentTenant.Change(command.TenantId);

        await logs.AddAsync(
            new SystemLogDocument
            {
                TenantId = command.TenantId,
                Level = command.Level,
                Message = command.Message,
                Source = command.Source,
                Timestamp = command.Timestamp
            },
            cancellationToken);

        return Result.Success();
    }
}

public sealed record SystemLogResponse(
    Guid Id,
    Guid TenantId,
    string Level,
    string Message,
    string? Source,
    DateTimeOffset Timestamp);

public sealed record ListSystemLogsQuery(Guid TenantId, string? Level = null, int Take = 100)
    : IQuery<IReadOnlyList<SystemLogResponse>>;

public sealed class ListSystemLogsQueryValidator : AbstractValidator<ListSystemLogsQuery>
{
    public ListSystemLogsQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Take).InclusiveBetween(1, 500);
    }
}

public sealed class ListSystemLogsQueryHandler(
    ISystemLogRepository logs,
    ICurrentTenant currentTenant) : IQueryHandler<ListSystemLogsQuery, IReadOnlyList<SystemLogResponse>>
{
    public async Task<Result<IReadOnlyList<SystemLogResponse>>> Handle(
        ListSystemLogsQuery query,
        CancellationToken cancellationToken)
    {
        using IDisposable scope = currentTenant.Change(query.TenantId);

        IReadOnlyList<SystemLogDocument> documents = await logs.ListAsync(
            query.TenantId,
            query.Level,
            query.Take,
            cancellationToken);

        return documents
            .Select(document => new SystemLogResponse(
                document.Id,
                document.TenantId,
                document.Level,
                document.Message,
                document.Source,
                document.Timestamp))
            .ToList();
    }
}
