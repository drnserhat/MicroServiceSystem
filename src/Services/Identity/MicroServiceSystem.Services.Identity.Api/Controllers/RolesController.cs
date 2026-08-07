using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Identity.Application.Roles;
using MicroServiceSystem.Services.Identity.Application.Users;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.IdentityRolesRead)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListRolesQuery(), cancellationToken));

    [HttpPost]
    [HasPermission(FrameworkPermissions.IdentityRolesWrite)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        return ToCreatedResult(
            await sender.Send(
                new CreateRoleCommand(request.Name, request.Permissions ?? []),
                cancellationToken),
            nameof(List));
    }

    [HttpPut("{roleId:guid}")]
    [HasPermission(FrameworkPermissions.IdentityRolesWrite)]
    public async Task<IActionResult> Replace(
        Guid roleId,
        [FromBody] ReplaceRoleRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(
            await sender.Send(
                new ReplaceRoleCommand(roleId, request.Name, request.Permissions ?? []),
                cancellationToken));

    [HttpDelete("{roleId:guid}")]
    [HasPermission(FrameworkPermissions.IdentityRolesWrite)]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new DeleteRoleCommand(roleId), cancellationToken));
}

public sealed record CreateRoleRequest(string Name, string[] Permissions);

public sealed record ReplaceRoleRequest(string Name, string[] Permissions);
