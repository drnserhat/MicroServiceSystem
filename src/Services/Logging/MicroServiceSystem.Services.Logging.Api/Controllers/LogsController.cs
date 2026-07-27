using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Logging.Application;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Logging.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/logs")]
public sealed class LogsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.LoggingLogsRead)]
    public async Task<IActionResult> List(
        [FromQuery] Guid tenantId,
        [FromQuery] string? level,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await sender.Send(new ListSystemLogsQuery(tenantId, level, take), cancellationToken));

    [HttpPost("ingest")]
    [HasPermission(FrameworkPermissions.LoggingLogsIngest)]
    public async Task<IActionResult> Ingest(IngestSystemLogCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
