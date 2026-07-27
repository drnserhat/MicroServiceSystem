using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Settings.Application;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Settings.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
public sealed class SettingsController(ISender sender) : ApiControllerBase
{
    [HttpGet("{key}")]
    [HasPermission(FrameworkPermissions.SettingsValuesRead)]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetSettingByKeyQuery(key), cancellationToken));

    [HttpPut]
    [HasPermission(FrameworkPermissions.SettingsValuesWrite)]
    public async Task<IActionResult> Put(UpsertSettingCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
