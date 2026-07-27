using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Audit;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Audit.Application;

// The tenant comes from the caller's token via TenantResolutionMiddleware and is stamped on the row by
// TenantAssignmentInterceptor. Accepting it from the request body would let any caller write into an
// arbitrary tenant.
public sealed record CreateAuditEntryCommand(string Action, string ResourceType, string ResourceId, Guid? ActorUserId, string? Details) : ICommand;

public sealed class CreateAuditEntryCommandValidator : AbstractValidator<CreateAuditEntryCommand>
{
    public CreateAuditEntryCommandValidator()
    {
        RuleFor(x => x.Action).NotEmpty();
        RuleFor(x => x.ResourceType).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
    }
}

public sealed class CreateAuditEntryCommandHandler(IAuditEntryRepository entries) : ICommandHandler<CreateAuditEntryCommand>
{
    public async Task<Result> Handle(CreateAuditEntryCommand c, CancellationToken ct)
    {
        var entry = AuditEntry.Create(c.Action, c.ResourceType, c.ResourceId, c.ActorUserId, c.Details);
        await entries.AddAsync(entry, ct);
        return Result.Success();
    }
}

public sealed class AuditEntryRequestedIntegrationEventHandler(IAuditEntryRepository entries, ICurrentTenant tenant)
    : IIntegrationEventHandler<AuditEntryRequestedIntegrationEvent>
{
    public async Task HandleAsync(AuditEntryRequestedIntegrationEvent e, CancellationToken ct = default)
    {
        if (e.TenantId is not Guid tenantId || tenantId == Guid.Empty)
        {
            return;
        }

        using IDisposable scope = tenant.Change(tenantId);
        var entry = AuditEntry.Create(e.Action, e.ResourceType, e.ResourceId, e.ActorUserId, e.Details);
        entry.TenantId = tenantId;
        await entries.AddAsync(entry, ct);
    }
}

public sealed record AuditEntryResponse(
    Guid Id,
    string Action,
    string ResourceType,
    string ResourceId,
    Guid? ActorUserId,
    string? Details);

public sealed record ListAuditEntriesQuery(PaginationRequest Pagination) : IQuery<PagedResult<AuditEntryResponse>>;

public sealed class ListAuditEntriesQueryValidator : AbstractValidator<ListAuditEntriesQuery>
{
    public ListAuditEntriesQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);
        RuleFor(query => query.Pagination.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
    }
}

public sealed class ListAuditEntriesQueryHandler(IAuditEntryRepository entries)
    : IQueryHandler<ListAuditEntriesQuery, PagedResult<AuditEntryResponse>>
{
    public async Task<Result<PagedResult<AuditEntryResponse>>> Handle(
        ListAuditEntriesQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<AuditEntry> page = await entries.PagedListAsync(query.Pagination, cancellationToken);
        return page.Project(entry => new AuditEntryResponse(
            entry.Id,
            entry.Action,
            entry.ResourceType,
            entry.ResourceId,
            entry.ActorUserId,
            entry.Details));
    }
}
