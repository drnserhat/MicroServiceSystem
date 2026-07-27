using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Notification;
using MicroServiceSystem.Services.Notification.Application.Abstractions;
using MicroServiceSystem.Services.Notification.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Notification.Application;
// Tenant comes from the caller's token, not the request body.
public sealed record CreateNotificationCommand(Guid UserId,string Email,string DisplayName,string Channel):ICommand;
public sealed class CreateNotificationCommandValidator:AbstractValidator<CreateNotificationCommand>{public CreateNotificationCommandValidator(){RuleFor(x=>x.UserId).NotEmpty();RuleFor(x=>x.Email).EmailAddress();RuleFor(x=>x.DisplayName).NotEmpty();}}
public sealed class CreateNotificationCommandHandler(INotificationMessageRepository messages,IPushSender pushSender):ICommandHandler<CreateNotificationCommand>{public async Task<Result> Handle(CreateNotificationCommand c,CancellationToken ct){var message=NotificationMessage.Create(c.UserId,c.Email,c.DisplayName,c.Channel);await messages.AddAsync(message,ct);await pushSender.SendWelcomeAsync(message.UserId,message.Email,message.DisplayName,ct);message.MarkSent();messages.Update(message);return Result.Success();}}
public sealed class WelcomeNotificationRequestedIntegrationEventHandler(INotificationMessageRepository messages,IPushSender sender,ICurrentTenant tenant):IIntegrationEventHandler<WelcomeNotificationRequestedIntegrationEvent>{public async Task HandleAsync(WelcomeNotificationRequestedIntegrationEvent e,CancellationToken ct=default){if(e.TenantId is not Guid tenantId||tenantId==Guid.Empty){return;}using IDisposable scope=tenant.Change(tenantId);var m=NotificationMessage.Create(e.UserId,e.Email,e.DisplayName);m.TenantId=tenantId;await messages.AddAsync(m,ct);await sender.SendWelcomeAsync(m.UserId,m.Email,m.DisplayName,ct);m.MarkSent();messages.Update(m);}}
