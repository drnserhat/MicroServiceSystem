using Asp.Versioning;
using Coordinator.Application.Registration;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Api.Controllers;

/// <summary>
/// Registers a user through the RegisterUser saga. Closed to anonymous callers: the caller must hold
/// <see cref="FrameworkPermissions.RegistrationUsersCreate"/> and the body <c>TenantId</c> must match
/// the JWT tenant (catalog membership is still enforced in the handler).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/registration")]
public sealed class RegistrationController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    [HasPermission(FrameworkPermissions.RegistrationUsersCreate)]
    public async Task<IActionResult> Register(
        [FromBody] RegistrationRequest request,
        CancellationToken cancellationToken)
    {
        Result<StartRegisterUserSagaResponse> result = await sender.Send(
            new StartRegisterUserSagaCommand(
                request.Email,
                request.UserName,
                request.Password,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }
}

public sealed record RegistrationRequest(
    string Email,
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid TenantId);
