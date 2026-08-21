using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Identity.Application.TenantDatabases;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants/{tenantId:guid}/databases")]
[TenantIndependent]
public sealed class TenantDatabasesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(FrameworkPermissions.IdentityTenantDatabasesRead)]
    public async Task<IActionResult> List(Guid tenantId, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ListTenantDatabasesQuery(tenantId), cancellationToken));

    [HttpPost("{serviceKey}/provision")]
    [HasPermission(FrameworkPermissions.IdentityTenantDatabasesWrite)]
    public async Task<IActionResult> Provision(
        Guid tenantId,
        string serviceKey,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ProvisionTenantDatabaseCommand(tenantId, serviceKey), cancellationToken));

    [HttpPost("{serviceKey}/retry")]
    [HasPermission(FrameworkPermissions.IdentityTenantDatabasesWrite)]
    public async Task<IActionResult> Retry(
        Guid tenantId,
        string serviceKey,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new RetryTenantDatabaseCommand(tenantId, serviceKey), cancellationToken));

    [HttpPost("{serviceKey}/health")]
    [HasPermission(FrameworkPermissions.IdentityTenantDatabasesWrite)]
    public async Task<IActionResult> Health(
        Guid tenantId,
        string serviceKey,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new HealthTenantDatabaseCommand(tenantId, serviceKey), cancellationToken));

    [AuthorizeInternalService]
    [HttpGet("{serviceKey}/binding")]
    public async Task<IActionResult> ResolveBinding(
        Guid tenantId,
        string serviceKey,
        CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new ResolveTenantDatabaseBindingQuery(tenantId, serviceKey), cancellationToken));
}
