using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Http;
using MicroServiceSystem.Services.Settings.Application;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Settings.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
public sealed class SettingsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.SettingsValuesRead)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListSettingsQuery(pagination), cancellationToken));

    [HttpGet("{key}")]
    [HasPermission(FrameworkPermissions.SettingsValuesRead)]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        Result<SettingResponse> result = await sender.Send(new GetSettingByKeyQuery(key), cancellationToken);
        return ToActionResultWithETag(result, setting => setting.Version);
    }

    [HttpPut]
    [HasPermission(FrameworkPermissions.SettingsValuesWrite)]
    public async Task<IActionResult> Put(
        [FromBody] UpsertSettingRequest request,
        CancellationToken cancellationToken)
    {
        uint? expectedVersion = EntityTag.TryGetIfMatch(Request, out uint version) ? version : null;

        Result<SettingResponse> result = await sender.Send(
            new UpsertSettingCommand(request.Key, request.Value, expectedVersion),
            cancellationToken);

        if (result.IsFailure && result.Error.Code == SettingsErrors.ConcurrencyTokenRequired.Code)
        {
            return MissingIfMatch();
        }

        return ToActionResultWithETag(result, setting => setting.Version);
    }

    [HttpDelete("{key}")]
    [HasPermission(FrameworkPermissions.SettingsValuesWrite)]
    public async Task<IActionResult> Delete(string key, CancellationToken cancellationToken)
    {
        if (!EntityTag.TryGetIfMatch(Request, out uint expectedVersion))
        {
            return MissingIfMatch();
        }

        return ToActionResult(
            await sender.Send(new DeleteSettingCommand(key, expectedVersion), cancellationToken));
    }
}

public sealed record UpsertSettingRequest(string Key, string Value);
