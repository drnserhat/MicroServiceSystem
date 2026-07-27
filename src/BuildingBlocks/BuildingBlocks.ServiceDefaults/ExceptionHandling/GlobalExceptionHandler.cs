using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Primitives;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.ExceptionHandling;

/// <summary>
/// Turns unexpected failures into a stable ProblemDetails payload and maps known domain failures to
/// the correct status codes so clients never see an unhandled stack dump.
/// </summary>
public sealed class GlobalExceptionHandler(IHostEnvironment environment, IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string title, string detail, string code) = Map(exception);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = environment.IsDevelopment() || exception is DomainException or TenantResolutionException
                ? detail
                : "An unexpected error occurred.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                ["code"] = code,
                ["timestampUtc"] = DateTimeOffset.UtcNow
            }
        };

        if (exception is BusinessRuleValidationException businessRule)
        {
            problemDetails.Extensions["rule"] = businessRule.BrokenRule.GetType().Name;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title, string Detail, string Code) Map(Exception exception) =>
        exception switch
        {
            BusinessRuleValidationException businessRule =>
                (StatusCodes.Status422UnprocessableEntity, "Business rule violated", businessRule.Message, businessRule.Code),

            ConcurrencyConflictException concurrency =>
                (StatusCodes.Status409Conflict, "Concurrency conflict", concurrency.Message, concurrency.Code),

            DomainException domainException =>
                (StatusCodes.Status400BadRequest, "Domain error", domainException.Message, domainException.Code),

            TenantResolutionException tenantException =>
                (StatusCodes.Status400BadRequest, "Tenant resolution failed", tenantException.Message, FrameworkErrorCodes.TenantMissing),

            UnauthorizedAccessException unauthorized =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", unauthorized.Message, FrameworkErrorCodes.Unauthorized),

            KeyNotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Not found", notFound.Message, FrameworkErrorCodes.NotFound),

            ArgumentException argument =>
                (StatusCodes.Status400BadRequest, "Invalid argument", argument.Message, FrameworkErrorCodes.Validation),

            _ => (StatusCodes.Status500InternalServerError, "Internal server error", exception.Message, FrameworkErrorCodes.Unexpected)
        };
}
