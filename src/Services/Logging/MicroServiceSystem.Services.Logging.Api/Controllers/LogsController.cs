using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Logging.Application;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Logging.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/logs")]
public sealed class LogsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.LoggingLogsRead)]
    public async Task<IActionResult> List(
        [FromQuery] string? level,
        [FromQuery] string? source,
        [FromQuery] string? correlationId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await sender.Send(
            new ListSystemLogsQuery(level, source, correlationId, fromUtc, toUtc, pagination),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(FrameworkPermissions.LoggingLogsRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetSystemLogByIdQuery(id), cancellationToken));

    [HttpPost("ingest")]
    [HasPermission(FrameworkPermissions.LoggingLogsIngest)]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestSystemLogCommand command,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
