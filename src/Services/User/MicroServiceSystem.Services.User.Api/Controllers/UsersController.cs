using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Http;
using MicroServiceSystem.Services.User.Application.Profiles.Create;
using MicroServiceSystem.Services.User.Application.Profiles.Deactivate;
using MicroServiceSystem.Services.User.Application.Profiles.GetById;
using MicroServiceSystem.Services.User.Application.Profiles.Update;
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

        return ToActionResultWithETag(result, profile => profile.Version);
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

        return ToActionResultWithETag(result, profile => profile.Version);
    }

    [HttpPut("profiles/{id:guid}")]
    [HasPermission(FrameworkPermissions.UsersProfilesUpdate)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!EntityTag.TryGetIfMatch(Request, out uint expectedVersion))
        {
            return MissingIfMatch();
        }

        Result<UserProfileResponse> result = await sender.Send(
            new UpdateUserProfileCommand(
                id,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                expectedVersion),
            cancellationToken);

        return ToActionResultWithETag(result, profile => profile.Version);
    }
}

public sealed record CreateProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid TenantId);

public sealed record UpdateProfileRequest(string FirstName, string LastName, string? DisplayName);

public sealed record DeactivateProfileRequest(string Reason, Guid TenantId);
