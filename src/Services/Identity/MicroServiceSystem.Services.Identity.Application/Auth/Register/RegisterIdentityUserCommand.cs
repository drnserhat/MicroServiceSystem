using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Tenants;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Auth.Register;

/// <summary>
/// <paramref name="UserId"/> is chosen by the caller (the registration saga) so that retrying a call
/// whose response was lost re-targets the same user instead of creating a second one.
/// </summary>
public sealed record RegisterIdentityUserCommand(
    Guid UserId,
    string Email,
    string UserName,
    string Password,
    Guid TenantId) : ICommand<RegisterIdentityUserResponse>;

public sealed record RegisterIdentityUserResponse(Guid UserId, string Email, string UserName);

public sealed class RegisterIdentityUserCommandValidator : AbstractValidator<RegisterIdentityUserCommand>
{
    public RegisterIdentityUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
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
    ITenantStore tenants,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<RegisterIdentityUserCommand, RegisterIdentityUserResponse>
{
    public async Task<Result<RegisterIdentityUserResponse>> Handle(
        RegisterIdentityUserCommand command,
        CancellationToken cancellationToken)
    {
        Result<TenantInfo> tenant =
            await TenantAccess.RequireActiveAsync(tenants, command.TenantId, cancellationToken);

        if (tenant.IsFailure)
        {
            return Result.Failure<RegisterIdentityUserResponse>(tenant.Error);
        }

        using IDisposable tenantScope = currentTenant.Change(command.TenantId, tenant.Value.Name);

        // The caller reserved this id before calling. Seeing it already present means an earlier attempt
        // succeeded but its response never arrived, so replay the original outcome instead of failing.
        if (await users.GetByIdAsync(command.UserId, cancellationToken) is { } alreadyRegistered)
        {
            return new RegisterIdentityUserResponse(
                alreadyRegistered.Id,
                alreadyRegistered.Email,
                alreadyRegistered.UserName);
        }

        if (await users.FindByEmailAsync(command.Email, cancellationToken) is not null)
        {
            return IdentityErrors.EmailAlreadyRegistered;
        }

        if (await users.FindByUserNameAsync(command.UserName, cancellationToken) is not null)
        {
            return IdentityErrors.UserNameAlreadyTaken;
        }

        IdentityUser user = IdentityUser.Register(
            command.UserId,
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
