using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Identity.Application.Tenants;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Pagination;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

/// <summary>
/// Tenant catalog. Internal-service endpoints remain for saga/bootstrap; JWT admin endpoints power the admin SPA.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
[TenantIndependent]
public sealed class TenantsController(ISender sender) : ApiControllerBase
{
    [AuthorizeInternalService]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(
            new CreateTenantCommand(request.Name, request.Slug, request.TenantId),
            cancellationToken));

    [AuthorizeInternalService]
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetTenantQuery(tenantId), cancellationToken));

    [HttpGet]
    [HasPermission(FrameworkPermissions.IdentityTenantsRead)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListTenantsQuery(pagination), cancellationToken));

    [HttpPost("admin")]
    [HasPermission(FrameworkPermissions.IdentityTenantsWrite)]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(
            new CreateTenantCommand(request.Name, request.Slug, request.TenantId),
            cancellationToken));

    [HttpPost("{tenantId:guid}/activation")]
    [HasPermission(FrameworkPermissions.IdentityTenantsWrite)]
    public async Task<IActionResult> SetActivation(
        Guid tenantId,
        [FromBody] SetTenantActivationRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(
            new SetTenantActivationCommand(tenantId, request.IsActive),
            cancellationToken));
}

public sealed record CreateTenantRequest(string Name, string Slug, Guid? TenantId = null);

public sealed record SetTenantActivationRequest(bool IsActive);
