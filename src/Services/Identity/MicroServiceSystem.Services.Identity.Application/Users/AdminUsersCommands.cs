using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Abstractions;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Contracts.Events.Identity;
using MicroServiceSystem.Services.Identity.Application.Abstractions;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;
using MicroServiceSystem.SharedKernel.Specifications;

namespace MicroServiceSystem.Services.Identity.Application.Users;

public sealed record IdentityUserResponse(
    Guid Id,
    string Email,
    string UserName,
    bool IsActive,
    IReadOnlyList<Guid> RoleIds);

public sealed record RoleResponse(Guid Id, string Name, IReadOnlyList<string> Permissions);

public sealed record ListIdentityUsersQuery(PaginationRequest Pagination)
    : IQuery<PagedResult<IdentityUserResponse>>;

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleResponse>>;

public sealed record AdminDisableUserCommand(Guid UserId, string Reason) : ICommand;

public sealed record AdminAssignUserRoleCommand(Guid UserId, Guid RoleId) : ICommand;

public sealed record AdminUnassignUserRoleCommand(Guid UserId, Guid RoleId) : ICommand;

public sealed class ListIdentityUsersQueryValidator : AbstractValidator<ListIdentityUsersQuery>
{
    public ListIdentityUsersQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);
        RuleFor(query => query.Pagination.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
    }
}

public sealed class AdminDisableUserCommandValidator : AbstractValidator<AdminDisableUserCommand>
{
    public AdminDisableUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(512);
    }
}

public sealed class AdminAssignUserRoleCommandValidator : AbstractValidator<AdminAssignUserRoleCommand>
{
    public AdminAssignUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleId).NotEmpty();
    }
}

public sealed class AdminUnassignUserRoleCommandValidator : AbstractValidator<AdminUnassignUserRoleCommand>
{
    public AdminUnassignUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleId).NotEmpty();
    }
}

public sealed class ListIdentityUsersQueryHandler(IIdentityUserRepository users)
    : IQueryHandler<ListIdentityUsersQuery, PagedResult<IdentityUserResponse>>
{
    public async Task<Result<PagedResult<IdentityUserResponse>>> Handle(
        ListIdentityUsersQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<IdentityUser> page = await users.PagedListAsync(
            new IdentityUserSearchSpecification(query.Pagination.Search),
            query.Pagination,
            cancellationToken);

        return page.Project(user => new IdentityUserResponse(
            user.Id,
            user.Email,
            user.UserName,
            user.IsActive,
            user.RoleIds.ToArray()));
    }
}

public sealed class ListRolesQueryHandler(IRoleRepository roles)
    : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleResponse>>
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        ListRolesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Role> items = await roles.ListAsync(new RoleListSpecification(), cancellationToken);

        return items
            .Select(role => new RoleResponse(role.Id, role.Name, role.Permissions.ToArray()))
            .ToArray();
    }
}

public sealed class AdminDisableUserCommandHandler(
    IIdentityUserRepository users,
    ICurrentTenant currentTenant,
    IIntegrationEventPublisher integrationEvents) : ICommandHandler<AdminDisableUserCommand>
{
    public async Task<Result> Handle(AdminDisableUserCommand command, CancellationToken cancellationToken)
    {
        if (currentTenant.Id is not Guid tenantId)
        {
            return Result.Failure(IdentityErrors.TenantNotFound);
        }

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
                TenantId = tenantId
            },
            cancellationToken);

        return Result.Success();
    }
}

public sealed class AdminAssignUserRoleCommandHandler(
    IIdentityUserRepository users,
    IRoleRepository roles) : ICommandHandler<AdminAssignUserRoleCommand>
{
    public async Task<Result> Handle(AdminAssignUserRoleCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        Role? role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(IdentityErrors.RoleNotFound);
        }

        user.AssignRole(role.Id);
        users.Update(user);
        return Result.Success();
    }
}

public sealed class AdminUnassignUserRoleCommandHandler(
    IIdentityUserRepository users,
    IRoleRepository roles) : ICommandHandler<AdminUnassignUserRoleCommand>
{
    public async Task<Result> Handle(AdminUnassignUserRoleCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        Role? role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(IdentityErrors.RoleNotFound);
        }

        if (user.RoleIds.Contains(role.Id)
            && role.NormalizedName == FrameworkPermissions.AdminRoleName.ToUpperInvariant())
        {
            int activeAdmins = await users.CountActiveUsersWithRoleAsync(role.Id, cancellationToken);
            if (user.IsActive && activeAdmins <= 1)
            {
                return Result.Failure(IdentityErrors.LastAdminProtected);
            }
        }

        user.RemoveRole(role.Id);
        users.Update(user);
        return Result.Success();
    }
}

file sealed class IdentityUserSearchSpecification : Specification<IdentityUser>
{
    public IdentityUserSearchSpecification(string? search)
    {
        ApplyNoTracking();
        OrderBy(user => user.Email);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            Where(user =>
                user.Email.Contains(term) || user.UserName.ToLower().Contains(term));
        }
    }
}

file sealed class RoleListSpecification : Specification<Role>
{
    public RoleListSpecification()
    {
        ApplyNoTracking();
        OrderBy(role => role.Name);
    }
}
