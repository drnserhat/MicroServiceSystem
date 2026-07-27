using Asp.Versioning;
using Coordinator.Application.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/registration")]
public sealed class RegistrationController(ISender sender) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost]
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
