using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Http;
using MicroServiceSystem.Services.Location.Application;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Location.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/countries")]
public sealed class CountriesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.LocationCountriesRead)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListCountriesQuery(pagination), cancellationToken));

    [HttpGet("{code}")]
    [HasPermission(FrameworkPermissions.LocationCountriesRead)]
    public async Task<IActionResult> Get(string code, CancellationToken cancellationToken)
    {
        Result<CountryResponse> result = await sender.Send(new GetCountryByCodeQuery(code), cancellationToken);
        return ToActionResultWithETag(result, country => country.Version);
    }

    [HttpPost]
    [HasPermission(FrameworkPermissions.LocationCountriesCreate)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCountryRequest request,
        CancellationToken cancellationToken)
    {
        Result<CountryResponse> result = await sender.Send(
            new CreateCountryCommand(request.Code, request.Name),
            cancellationToken);
        return ToActionResultWithETag(result, country => country.Version);
    }

    [HttpPut("{code}")]
    [HasPermission(FrameworkPermissions.LocationCountriesWrite)]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateCountryRequest request,
        CancellationToken cancellationToken)
    {
        if (!EntityTag.TryGetIfMatch(Request, out uint expectedVersion))
        {
            return MissingIfMatch();
        }

        Result<CountryResponse> result = await sender.Send(
            new UpdateCountryCommand(code, request.Name, expectedVersion),
            cancellationToken);

        return ToActionResultWithETag(result, country => country.Version);
    }

    [HttpDelete("{code}")]
    [HasPermission(FrameworkPermissions.LocationCountriesWrite)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        if (!EntityTag.TryGetIfMatch(Request, out uint expectedVersion))
        {
            return MissingIfMatch();
        }

        return ToActionResult(
            await sender.Send(new DeleteCountryCommand(code, expectedVersion), cancellationToken));
    }
}

public sealed record CreateCountryRequest(string Code, string Name);

public sealed record UpdateCountryRequest(string Name);
