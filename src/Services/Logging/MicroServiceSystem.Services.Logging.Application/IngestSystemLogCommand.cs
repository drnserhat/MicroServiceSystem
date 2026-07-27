using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Logging.Application.Abstractions;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Logging.Application;

/// <summary>
/// The tenant is taken from the caller's token rather than the request body; documents in Mongo are not
/// covered by the EF tenant interceptor, so the handler stamps it explicitly.
/// </summary>
public sealed record IngestSystemLogCommand(
    string Level,
    string Message,
    string? Source,
    string? CorrelationId,
    DateTimeOffset Timestamp) : ICommand;

public sealed class IngestSystemLogCommandValidator : AbstractValidator<IngestSystemLogCommand>
{
    public IngestSystemLogCommandValidator()
    {
        RuleFor(command => command.Level).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Message).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Source).MaximumLength(128);
        RuleFor(command => command.CorrelationId).MaximumLength(128);
    }
}

public sealed class IngestSystemLogCommandHandler(
    ISystemLogRepository logs,
    ICurrentTenant currentTenant) : ICommandHandler<IngestSystemLogCommand>
{
    public async Task<Result> Handle(IngestSystemLogCommand command, CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid tenantId)
        {
            return Result.Failure(FrameworkErrors.TenantMissing());
        }

        await logs.AddAsync(
            new SystemLogDocument
            {
                TenantId = tenantId,
                Level = command.Level.Trim(),
                Message = command.Message,
                Source = string.IsNullOrWhiteSpace(command.Source) ? null : command.Source.Trim(),
                CorrelationId = string.IsNullOrWhiteSpace(command.CorrelationId)
                    ? null
                    : command.CorrelationId.Trim(),
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
    string? CorrelationId,
    DateTimeOffset Timestamp);

public sealed record GetSystemLogByIdQuery(Guid Id) : IQuery<SystemLogResponse>;

public sealed class GetSystemLogByIdQueryHandler(
    ISystemLogRepository logs,
    ICurrentTenant currentTenant) : IQueryHandler<GetSystemLogByIdQuery, SystemLogResponse>
{
    public async Task<Result<SystemLogResponse>> Handle(
        GetSystemLogByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid tenantId)
        {
            return FrameworkErrors.TenantMissing();
        }

        SystemLogDocument? document = await logs.FindByIdAsync(tenantId, query.Id, cancellationToken);
        return document is null ? LoggingErrors.NotFound : ToResponse(document);
    }

    private static SystemLogResponse ToResponse(SystemLogDocument document) =>
        new(
            document.Id,
            document.TenantId,
            document.Level,
            document.Message,
            document.Source,
            document.CorrelationId,
            document.Timestamp);
}

public sealed record ListSystemLogsQuery(
    string? Level,
    string? Source,
    string? CorrelationId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    PaginationRequest Pagination) : IQuery<PagedResult<SystemLogResponse>>;

public sealed class ListSystemLogsQueryValidator : AbstractValidator<ListSystemLogsQuery>
{
    public ListSystemLogsQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);
        RuleFor(query => query.Pagination.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(query => query.Level).MaximumLength(32);
        RuleFor(query => query.Source).MaximumLength(128);
        RuleFor(query => query.CorrelationId).MaximumLength(128);
    }
}

public sealed class ListSystemLogsQueryHandler(
    ISystemLogRepository logs,
    ICurrentTenant currentTenant) : IQueryHandler<ListSystemLogsQuery, PagedResult<SystemLogResponse>>
{
    public async Task<Result<PagedResult<SystemLogResponse>>> Handle(
        ListSystemLogsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid tenantId)
        {
            return FrameworkErrors.TenantMissing();
        }

        if (query.FromUtc is DateTimeOffset from && query.ToUtc is DateTimeOffset to && from > to)
        {
            return LoggingErrors.InvalidTimeRange;
        }

        PagedResult<SystemLogDocument> page = await logs.PagedListAsync(
            tenantId,
            new SystemLogListFilter(
                query.Level,
                query.Source,
                query.CorrelationId,
                query.FromUtc,
                query.ToUtc),
            query.Pagination,
            cancellationToken);

        return page.Project(document => new SystemLogResponse(
            document.Id,
            document.TenantId,
            document.Level,
            document.Message,
            document.Source,
            document.CorrelationId,
            document.Timestamp));
    }
}
