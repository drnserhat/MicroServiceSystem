using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Identity.Application.Users;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersAdminController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.IdentityUsersRead)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListIdentityUsersQuery(pagination), cancellationToken));

    [HttpPost("{userId:guid}/disable")]
    [HasPermission(FrameworkPermissions.IdentityUsersDisable)]
    public async Task<IActionResult> Disable(
        Guid userId,
        [FromBody] AdminDisableUserRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new AdminDisableUserCommand(userId, request.Reason), cancellationToken));

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [HasPermission(FrameworkPermissions.IdentityRolesAssign)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new AdminAssignUserRoleCommand(userId, roleId), cancellationToken));

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [HasPermission(FrameworkPermissions.IdentityRolesAssign)]
    public async Task<IActionResult> UnassignRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new AdminUnassignUserRoleCommand(userId, roleId), cancellationToken));
}

public sealed record AdminDisableUserRequest(string Reason);
