using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Settings.Application.Abstractions;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Settings.Application;
public sealed record SettingResponse(Guid Id,string Key,string Value);
public sealed record GetSettingByKeyQuery(string Key):IQuery<SettingResponse>;
public sealed class GetSettingByKeyQueryHandler(ISettingRepository settings):IQueryHandler<GetSettingByKeyQuery,SettingResponse>{public async Task<Result<SettingResponse>> Handle(GetSettingByKeyQuery q,CancellationToken ct){var s=await settings.FindByKeyAsync(q.Key,ct);return s is null ? SettingsErrors.NotFound : new SettingResponse(s.Id,s.Key,s.Value);}}
public sealed record UpsertSettingCommand(string Key,string Value,Guid TenantId):ICommand<SettingResponse>;
public sealed class UpsertSettingCommandValidator:AbstractValidator<UpsertSettingCommand>{public UpsertSettingCommandValidator(){RuleFor(x=>x.Key).NotEmpty().MaximumLength(128);RuleFor(x=>x.Value).NotEmpty();RuleFor(x=>x.TenantId).NotEmpty();}}
public sealed class UpsertSettingCommandHandler(ISettingRepository settings,ICurrentTenant tenant):ICommandHandler<UpsertSettingCommand,SettingResponse>{public async Task<Result<SettingResponse>> Handle(UpsertSettingCommand c,CancellationToken ct){using IDisposable scope=tenant.Change(c.TenantId);var s=await settings.FindByKeyAsync(c.Key,ct);if(s is null){s=Setting.Create(c.Key,c.Value);s.TenantId=c.TenantId;await settings.AddAsync(s,ct);}else{ s.SetValue(c.Value);settings.Update(s);}return new SettingResponse(s.Id,s.Key,s.Value);}}
