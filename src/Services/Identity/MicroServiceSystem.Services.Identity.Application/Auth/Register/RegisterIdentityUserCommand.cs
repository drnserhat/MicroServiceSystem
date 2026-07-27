using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Auth.Register;

public sealed record RegisterIdentityUserCommand(
    string Email,
    string UserName,
    string Password,
    Guid TenantId) : ICommand<RegisterIdentityUserResponse>;

public sealed record RegisterIdentityUserResponse(Guid UserId, string Email, string UserName);

public sealed class RegisterIdentityUserCommandValidator : AbstractValidator<RegisterIdentityUserCommand>
{
    public RegisterIdentityUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.UserName).NotEmpty().MinimumLength(3).MaximumLength(128);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(command => command.TenantId).NotEmpty();
    }
}

public sealed class RegisterIdentityUserCommandHandler(
    IIdentityUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<RegisterIdentityUserCommand, RegisterIdentityUserResponse>
{
    public async Task<Result<RegisterIdentityUserResponse>> Handle(
        RegisterIdentityUserCommand command,
        CancellationToken cancellationToken)
    {
        using IDisposable tenantScope = currentTenant.Change(command.TenantId);

        if (await users.FindByEmailAsync(command.Email, cancellationToken) is not null)
        {
            return IdentityErrors.EmailAlreadyRegistered;
        }

        if (await users.FindByUserNameAsync(command.UserName, cancellationToken) is not null)
        {
            return IdentityErrors.UserNameAlreadyTaken;
        }

        IdentityUser user = IdentityUser.Register(
            command.Email,
            command.UserName,
            passwordHasher.Hash(command.Password));

        user.TenantId = command.TenantId;

        Role memberRole = await AccessTokenFactory.GetOrCreateMemberRoleAsync(
            roles,
            command.TenantId,
            cancellationToken);
        user.AssignRole(memberRole.Id);

        await users.AddAsync(user, cancellationToken);

        await integrationEvents.PublishAsync(
            new UserRegisteredIntegrationEvent
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                TenantId = command.TenantId
            },
            cancellationToken);

        return new RegisterIdentityUserResponse(user.Id, user.Email, user.UserName);
    }
}
