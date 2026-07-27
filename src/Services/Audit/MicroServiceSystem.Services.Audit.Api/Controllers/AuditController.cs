using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Audit.Application;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Audit.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
public sealed class AuditController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.AuditEntriesRead)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListAuditEntriesQuery(pagination), cancellationToken));

    [HttpPost]
    [HasPermission(FrameworkPermissions.AuditEntriesCreate)]
    public async Task<IActionResult> Create(CreateAuditEntryCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
