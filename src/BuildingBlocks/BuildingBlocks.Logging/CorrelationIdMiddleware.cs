using Microsoft.AspNetCore.Http;
using MicroServiceSystem.SharedKernel.Constants;
using Serilog.Context;

namespace MicroServiceSystem.BuildingBlocks.Logging;

/// <summary>
/// Guarantees every request carries a correlation id, echoes it to the caller and pushes it into the
/// log context so a single identifier ties together logs across services.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string LogPropertyName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = ResolveCorrelationId(context);

        context.Items[LogPropertyName] = correlationId;
        context.Response.Headers[FrameworkHeaders.CorrelationId] = correlationId;

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context) =>
        context.Request.Headers.TryGetValue(FrameworkHeaders.CorrelationId, out Microsoft.Extensions.Primitives.StringValues header)
        && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : context.TraceIdentifier;
}
