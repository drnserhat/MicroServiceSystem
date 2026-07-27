using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Results;
using MicroServiceSystem.Services.Identity.Application.Tenants;
using MicroServiceSystem.SharedKernel.Models;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

/// <summary>
/// Tenant catalog. Provisioning and lookup are internal-service only — public callers never invent a
/// tenant id; they carry one in a JWT claim. User registration through Coordinator requires an
/// authenticated tenant admin (not anonymous self-signup).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
[TenantIndependent]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    [AuthorizeInternalService]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        Result<TenantResponse> result = await sender.Send(
            new CreateTenantCommand(request.Name, request.Slug, request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    [AuthorizeInternalService]
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken cancellationToken)
    {
        Result<TenantResponse> result = await sender.Send(
            new GetTenantQuery(tenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(ApiResponse<T>.Success(result.Value, HttpContext.TraceIdentifier))
            : StatusCode(
                ResultHttpMapper.ToStatusCode(result.Error.Type),
                ApiResponse<T>.Failure(result.Error, HttpContext.TraceIdentifier));
}

public sealed record CreateTenantRequest(string Name, string Slug, Guid? TenantId = null);
