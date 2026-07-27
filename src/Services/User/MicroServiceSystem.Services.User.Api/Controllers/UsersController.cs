using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.User.Application.Profiles.Create;
using MicroServiceSystem.Services.User.Application.Profiles.Deactivate;
using MicroServiceSystem.Services.User.Application.Profiles.GetById;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.User.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    /// <summary>
    /// Called by Coordinator during RegisterUser. Requires the internal service API key.
    /// </summary>
    [AuthorizeInternalService]
    [HttpPost("profiles")]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserProfileResponse> result = await sender.Send(
            new CreateUserProfileCommand(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    [AuthorizeInternalService]
    [HttpPost("profiles/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProfile(
        Guid id,
        [FromBody] DeactivateProfileRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new DeactivateUserProfileCommand(id, request.Reason, request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("profiles/{id:guid}")]
    [HasPermission(FrameworkPermissions.UsersProfilesRead)]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        Result<UserProfileResponse> result = await sender.Send(
            new GetUserProfileByIdQuery(id),
            cancellationToken);

        return ToActionResult(result);
    }
}

public sealed record CreateProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid TenantId);

public sealed record DeactivateProfileRequest(string Reason, Guid TenantId);
