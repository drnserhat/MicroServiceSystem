using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Location.Application;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Location.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/countries")]
public sealed class CountriesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.LocationCountriesRead)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListCountriesQuery(), cancellationToken));

    [HttpPost]
    [HasPermission(FrameworkPermissions.LocationCountriesCreate)]
    public async Task<IActionResult> Create(CreateCountryCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
