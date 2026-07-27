using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Auth.Disable;

public sealed record DisableIdentityUserCommand(Guid UserId, string Reason, Guid TenantId) : ICommand;

public sealed class DisableIdentityUserCommandValidator : AbstractValidator<DisableIdentityUserCommand>
{
    public DisableIdentityUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(512);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class DisableIdentityUserCommandHandler(
    IIdentityUserRepository users,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<DisableIdentityUserCommand>
{
    public async Task<Result> Handle(DisableIdentityUserCommand command, CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        IdentityUser? user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        user.Disable(command.Reason);
        users.Update(user);

        await integrationEvents.PublishAsync(
            new UserDisabledIntegrationEvent
            {
                UserId = user.Id,
                Reason = command.Reason,
                TenantId = command.TenantId
            },
            cancellationToken);

        return Result.Success();
    }
}
