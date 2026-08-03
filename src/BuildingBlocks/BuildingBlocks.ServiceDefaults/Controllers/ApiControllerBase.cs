using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Localization.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Http;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Results;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Models;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<object?>.Success(null, HttpContext.TraceIdentifier));
        }

        return ToFailureResult<object?>(result.Error);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<T>.Success(result.Value, HttpContext.TraceIdentifier));
        }

        return ToFailureResult<T>(result.Error);
    }

    /// <summary>
    /// Returns the payload and sets a strong <c>ETag</c> from the resource version.
    /// </summary>
    protected IActionResult ToActionResultWithETag<T>(Result<T> result, Func<T, uint> versionSelector)
    {
        ArgumentNullException.ThrowIfNull(versionSelector);

        if (result.IsSuccess)
        {
            EntityTag.Set(Response, versionSelector(result.Value));
            return Ok(ApiResponse<T>.Success(result.Value, HttpContext.TraceIdentifier));
        }

        return ToFailureResult<T>(result.Error);
    }

    protected IActionResult ToCreatedResult<T>(Result<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            return CreatedAtAction(
                actionName,
                routeValues ?? new { id = result.Value },
                ApiResponse<T>.Success(result.Value, HttpContext.TraceIdentifier));
        }

        return ToFailureResult<T>(result.Error);
    }

    /// <summary>
    /// 428 when a mutating request omits a usable <c>If-Match</c> version.
    /// </summary>
    protected IActionResult MissingIfMatch() =>
        StatusCode(
            StatusCodes.Status428PreconditionRequired,
            ApiResponse<object?>.Failure(
                Localize(Error.Validation(
                    FrameworkErrorCodes.Validation,
                    "If-Match header with the current resource version is required.")),
                HttpContext.TraceIdentifier));

    private ObjectResult ToFailureResult<T>(Error error)
    {
        int statusCode = ResultHttpMapper.ToStatusCode(error.Type);

        return StatusCode(statusCode, ApiResponse<T>.Failure(Localize(error), HttpContext.TraceIdentifier));
    }

    private Error Localize(Error error)
    {
        IErrorLocalizer? localizer = HttpContext.RequestServices.GetService<IErrorLocalizer>();
        return localizer?.Localize(error) ?? error;
    }
}
