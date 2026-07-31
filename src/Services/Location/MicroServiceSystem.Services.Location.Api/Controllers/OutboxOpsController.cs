using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.Messaging.Abstractions;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Models;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Location.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ops/outbox")]
[TenantIndependent]
public sealed class OutboxOpsController(IOutboxRepository outbox) : ApiControllerBase
{
    public const string ServiceName = "location";

    [HttpGet]
    [HasPermission(FrameworkPermissions.OpsOutboxRead)]
    public async Task<IActionResult> Snapshot([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        int pendingCount = await outbox.CountPendingAsync(cancellationToken);
        int deadLetterCount = await outbox.CountDeadLetteredAsync(cancellationToken);
        IReadOnlyList<OutboxDeadLetterRow> deadLetters = await outbox.ListDeadLetteredAsync(take, cancellationToken);
        IReadOnlyList<OutboxPendingRow> pending = await outbox.ListPendingAsync(take, cancellationToken);

        return Ok(ApiResponse<OutboxOpsSnapshotDto>.Success(
            OutboxOpsSnapshotFactory.Create(ServiceName, pendingCount, deadLetterCount, deadLetters, pending),
            HttpContext.TraceIdentifier));
    }

    [HttpPost("{messageId:guid}/requeue")]
    [HasPermission(FrameworkPermissions.OpsOutboxWrite)]
    public async Task<IActionResult> Requeue(Guid messageId, CancellationToken cancellationToken)
    {
        bool requeued = await outbox.RequeueDeadLetteredAsync(messageId, cancellationToken);
        return requeued
            ? ToActionResult(Result.Success())
            : ToActionResult(Result.Failure(Error.NotFound("ops.outbox.not_found", "Dead-lettered outbox message was not found.")));
    }
}
