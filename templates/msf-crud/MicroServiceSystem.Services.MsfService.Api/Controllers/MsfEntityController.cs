using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.CreateMsfEntity;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.DeleteMsfEntity;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.UpdateMsfEntity;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Contracts;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.GetMsfEntityById;
using MicroServiceSystem.Services.MsfService.Application.MsfEntity.Queries.ListMsfEntity;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.MsfService.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/plural-route-segment")]
public sealed class MsfEntityController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission("permission-prefix.msfentity.read")]
    public async Task<IActionResult> ListAsync(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        Result<PagedResult<MsfEntityResponse>> result =
            await sender.Send(new ListMsfEntityQuery(pagination), cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("permission-prefix.msfentity.read")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<MsfEntityResponse> result = await sender.Send(new GetMsfEntityByIdQuery(id), cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [HasPermission("permission-prefix.msfentity.create")]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateMsfEntityCommand command,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await sender.Send(command, cancellationToken);

        return ToCreatedResult(result, nameof(GetByIdAsync));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("permission-prefix.msfentity.update")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateMsfEntityRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new UpdateMsfEntityCommand(id, request.Name, request.Description),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("permission-prefix.msfentity.delete")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new DeleteMsfEntityCommand(id), cancellationToken);

        return ToActionResult(result);
    }
}

public sealed record UpdateMsfEntityRequest(string Name, string? Description);
