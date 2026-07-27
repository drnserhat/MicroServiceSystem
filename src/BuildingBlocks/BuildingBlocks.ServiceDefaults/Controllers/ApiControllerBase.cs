using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Results;
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

    private ObjectResult ToFailureResult<T>(Error error)
    {
        int statusCode = ResultHttpMapper.ToStatusCode(error.Type);

        return StatusCode(statusCode, ApiResponse<T>.Failure(error, HttpContext.TraceIdentifier));
    }
}
