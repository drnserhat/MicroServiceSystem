using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Application.Users;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Application.Roles;

public sealed record CreateRoleCommand(string Name, IReadOnlyList<string> Permissions) : ICommand<RoleResponse>;

public sealed record ReplaceRoleCommand(Guid RoleId, string Name, IReadOnlyList<string> Permissions)
    : ICommand<RoleResponse>;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Permissions).NotNull();
    }
}

public sealed class ReplaceRoleCommandValidator : AbstractValidator<ReplaceRoleCommand>
{
    public ReplaceRoleCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Permissions).NotNull();
    }
}

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator() => RuleFor(command => command.RoleId).NotEmpty();
}

public sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    ICurrentTenant currentTenant) : ICommandHandler<CreateRoleCommand, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid tenantId)
        {
            return Result.Failure<RoleResponse>(IdentityErrors.TenantNotFound);
        }

        if (FrameworkPermissions.IsBuiltInRoleName(command.Name))
        {
            return Result.Failure<RoleResponse>(IdentityErrors.RoleNameReserved);
        }

        Result<IReadOnlyList<string>> permissions = RoleCommandSupport.NormalizePermissions(command.Permissions);
        if (permissions.IsFailure)
        {
            return Result.Failure<RoleResponse>(permissions.Error);
        }

        if (await roles.FindByNameAsync(command.Name, cancellationToken) is not null)
        {
            return Result.Failure<RoleResponse>(IdentityErrors.RoleNameTaken);
        }

        Role role = Role.CreateCustom(command.Name);
        role.TenantId = tenantId;
        role.ReplacePermissions(permissions.Value);

        await roles.AddAsync(role, cancellationToken);

        return RoleCommandSupport.ToResponse(role);
    }
}

public sealed class ReplaceRoleCommandHandler(IRoleRepository roles) : ICommandHandler<ReplaceRoleCommand, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(ReplaceRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleResponse>(IdentityErrors.RoleNotFound);
        }

        if (role.IsBuiltIn)
        {
            return Result.Failure<RoleResponse>(IdentityErrors.BuiltInRoleProtected);
        }

        if (FrameworkPermissions.IsBuiltInRoleName(command.Name))
        {
            return Result.Failure<RoleResponse>(IdentityErrors.RoleNameReserved);
        }

        Result<IReadOnlyList<string>> permissions = RoleCommandSupport.NormalizePermissions(command.Permissions);
        if (permissions.IsFailure)
        {
            return Result.Failure<RoleResponse>(permissions.Error);
        }

        Role? nameOwner = await roles.FindByNameAsync(command.Name, cancellationToken);
        if (nameOwner is not null && nameOwner.Id != role.Id)
        {
            return Result.Failure<RoleResponse>(IdentityErrors.RoleNameTaken);
        }

        role.Rename(command.Name);
        role.ReplacePermissions(permissions.Value);
        roles.Update(role);

        return RoleCommandSupport.ToResponse(role);
    }
}

public sealed class DeleteRoleCommandHandler(
    IRoleRepository roles,
    IIdentityUserRepository users) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(IdentityErrors.RoleNotFound);
        }

        if (role.IsBuiltIn)
        {
            return Result.Failure(IdentityErrors.BuiltInRoleProtected);
        }

        int assignees = await users.CountUsersWithRoleAsync(role.Id, cancellationToken);
        if (assignees > 0)
        {
            return Result.Failure(IdentityErrors.RoleInUse);
        }

        role.EnsureCanDelete();
        roles.Remove(role);

        return Result.Success();
    }
}

file static class RoleCommandSupport
{
    public static Result<IReadOnlyList<string>> NormalizePermissions(IReadOnlyList<string> permissions)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string raw in permissions)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            string code = raw.Trim();
            if (!FrameworkPermissions.KnownPermissionCodes.Contains(code))
            {
                return Result.Failure<IReadOnlyList<string>>(IdentityErrors.UnknownPermission);
            }

            if (seen.Add(code))
            {
                string canonical = FrameworkPermissions.KnownPermissionCodes
                    .First(known => string.Equals(known, code, StringComparison.OrdinalIgnoreCase));
                normalized.Add(canonical);
            }
        }

        return normalized;
    }

    public static RoleResponse ToResponse(Role role) =>
        new(role.Id, role.Name, role.Permissions.ToArray());
}
