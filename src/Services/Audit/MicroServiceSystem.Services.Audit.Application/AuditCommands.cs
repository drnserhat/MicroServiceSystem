using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Audit;
using MicroServiceSystem.Services.Audit.Application.Abstractions;
using MicroServiceSystem.Services.Audit.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Audit.Application;
public sealed record CreateAuditEntryCommand(string Action,string ResourceType,string ResourceId,Guid? ActorUserId,string? Details,Guid TenantId) : ICommand;
public sealed class CreateAuditEntryCommandValidator : AbstractValidator<CreateAuditEntryCommand> { public CreateAuditEntryCommandValidator(){RuleFor(x=>x.Action).NotEmpty();RuleFor(x=>x.ResourceType).NotEmpty();RuleFor(x=>x.ResourceId).NotEmpty();RuleFor(x=>x.TenantId).NotEmpty();} }
public sealed class CreateAuditEntryCommandHandler(IAuditEntryRepository entries, ICurrentTenant tenant) : ICommandHandler<CreateAuditEntryCommand> { public async Task<Result> Handle(CreateAuditEntryCommand c,CancellationToken ct){using IDisposable scope=tenant.Change(c.TenantId);var entry=AuditEntry.Create(c.Action,c.ResourceType,c.ResourceId,c.ActorUserId,c.Details);entry.TenantId=c.TenantId;await entries.AddAsync(entry,ct);return Result.Success();} }
public sealed class AuditEntryRequestedIntegrationEventHandler(IAuditEntryRepository entries, ICurrentTenant tenant) : IIntegrationEventHandler<AuditEntryRequestedIntegrationEvent> { public async Task HandleAsync(AuditEntryRequestedIntegrationEvent e,CancellationToken ct=default){if(e.TenantId is not Guid tenantId || tenantId==Guid.Empty){return;}using IDisposable scope=tenant.Change(tenantId);var entry=AuditEntry.Create(e.Action,e.ResourceType,e.ResourceId,e.ActorUserId,e.Details);entry.TenantId=tenantId;await entries.AddAsync(entry,ct);} }
public sealed record AuditEntryResponse(Guid Id,string Action,string ResourceType,string ResourceId,Guid? ActorUserId,string? Details);
public sealed record ListAuditEntriesQuery : IQuery<IReadOnlyList<AuditEntryResponse>>;
public sealed class ListAuditEntriesQueryHandler(IAuditEntryRepository entries) : IQueryHandler<ListAuditEntriesQuery,IReadOnlyList<AuditEntryResponse>> { public async Task<Result<IReadOnlyList<AuditEntryResponse>>> Handle(ListAuditEntriesQuery q,CancellationToken ct){var list=await entries.ListAllAsync(ct);return list.Select(x=>new AuditEntryResponse(x.Id,x.Action,x.ResourceType,x.ResourceId,x.ActorUserId,x.Details)).ToList();} }
