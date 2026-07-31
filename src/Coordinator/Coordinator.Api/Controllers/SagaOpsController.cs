using Asp.Versioning;
using Coordinator.Application.Abstractions;
using Coordinator.Domain.Aggregates;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Models;
using MicroServiceSystem.SharedKernel.Results;

namespace Coordinator.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ops/sagas")]
[TenantIndependent]
public sealed class SagaOpsController(IRegisterUserSagaRepository sagas) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.OpsSagaRead)]
    public async Task<IActionResult> List(
        [FromQuery] string? state = null,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        IReadOnlyList<RegisterUserSaga> rows = await sagas.ListForOpsAsync(state, take, cancellationToken);
        SagaOpsListDto payload = new(rows.Select(ToDto).ToArray());
        return Ok(ApiResponse<SagaOpsListDto>.Success(payload, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(FrameworkPermissions.OpsSagaRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        RegisterUserSaga? saga = await sagas.GetByIdAsync(id, cancellationToken);
        if (saga is null)
        {
            return ToActionResult(Result.Failure(Error.NotFound("ops.saga.not_found", "Saga was not found.")));
        }

        return Ok(ApiResponse<SagaOpsItemDto>.Success(ToDto(saga), HttpContext.TraceIdentifier));
    }

    private static SagaOpsItemDto ToDto(RegisterUserSaga saga) =>
        new(
            saga.Id,
            "RegisterUser",
            saga.State.ToString(),
            DeriveCurrentStep(saga),
            saga.Email,
            saga.UserName,
            saga.DisplayName,
            saga.IdentityUserId,
            saga.UserProfileId,
            saga.FailureReason,
            saga.LockedBy,
            saga.LockedUntilUtc,
            saga.TenantId,
            saga.CreatedAtUtc,
            saga.ModifiedAtUtc,
            saga.IsTerminal);

    private static string DeriveCurrentStep(RegisterUserSaga saga) =>
        saga.State switch
        {
            RegisterUserSagaState.Started => "RegisterIdentityStep",
            RegisterUserSagaState.IdentityRegistered => "CreateUserProfileStep",
            RegisterUserSagaState.UserProfileCreated => "Completed",
            RegisterUserSagaState.Compensating => "Identity.Disable (compensation)",
            RegisterUserSagaState.Completed => "Completed",
            RegisterUserSagaState.Failed => "Failed",
            _ => saga.State.ToString()
        };
}

public sealed record SagaOpsListDto(IReadOnlyList<SagaOpsItemDto> Items);

public sealed record SagaOpsItemDto(
    Guid Id,
    string Name,
    string State,
    string CurrentStep,
    string Email,
    string UserName,
    string DisplayName,
    Guid? IdentityUserId,
    Guid? UserProfileId,
    string? FailureReason,
    string? LockedBy,
    DateTimeOffset? LockedUntilUtc,
    Guid TenantId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc,
    bool IsTerminal);
