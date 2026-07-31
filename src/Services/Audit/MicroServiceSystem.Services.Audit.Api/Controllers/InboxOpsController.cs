using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Models;

namespace MicroServiceSystem.Services.Audit.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ops/inbox")]
[TenantIndependent]
public sealed class InboxOpsController(IInboxRepository inbox) : ApiControllerBase
{
    public const string ServiceName = "audit";

    [HttpGet("summary")]
    [HasPermission(FrameworkPermissions.OpsInboxRead)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        InboxOpsSummaryDto dto = new(
            ServiceName,
            await inbox.CountProcessedAsync(cancellationToken),
            await inbox.CountOpenAsync(cancellationToken),
            await inbox.CountInFlightAsync(utcNow, cancellationToken),
            await inbox.CountFailedAsync(cancellationToken));
        return Ok(ApiResponse<InboxOpsSummaryDto>.Success(dto, HttpContext.TraceIdentifier));
    }
}
