using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Results;
using MicroServiceSystem.Services.Identity.Application.Auth.Disable;
using MicroServiceSystem.Services.Identity.Application.Auth.Login;
using MicroServiceSystem.Services.Identity.Application.Auth.Refresh;
using MicroServiceSystem.Services.Identity.Application.Auth.Register;
using MicroServiceSystem.SharedKernel.Models;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        Result<RegisterIdentityUserResponse> result = await sender.Send(
            new RegisterIdentityUserCommand(request.Email, request.UserName, request.Password, request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        Result<LoginResponse> result = await sender.Send(
            new LoginCommand(
                request.Email,
                request.Password,
                request.TenantId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        Result<LoginResponse> result = await sender.Send(
            new RefreshTokenCommand(request.RefreshToken, request.TenantId),
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Called by Coordinator during RegisterUser compensation. Requires the internal service API key.
    /// </summary>
    [AuthorizeInternalService]
    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] DisableRequest request, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new DisableIdentityUserCommand(request.UserId, request.Reason, request.TenantId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object?>.Success(null, HttpContext.TraceIdentifier))
            : StatusCode(ResultHttpMapper.ToStatusCode(result.Error.Type), ApiResponse<object?>.Failure(result.Error, HttpContext.TraceIdentifier));
    }

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(ApiResponse<T>.Success(result.Value, HttpContext.TraceIdentifier))
            : StatusCode(
                ResultHttpMapper.ToStatusCode(result.Error.Type),
                ApiResponse<T>.Failure(result.Error, HttpContext.TraceIdentifier));
}

public sealed record RegisterRequest(string Email, string UserName, string Password, Guid TenantId);

public sealed record LoginRequest(string Email, string Password, Guid TenantId);

public sealed record RefreshRequest(string RefreshToken, Guid TenantId);

public sealed record DisableRequest(Guid UserId, string Reason, Guid TenantId);
