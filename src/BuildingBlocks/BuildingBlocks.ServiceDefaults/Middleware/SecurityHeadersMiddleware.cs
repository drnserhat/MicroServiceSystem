using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Configuration;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.Middleware;

/// <summary>
/// Applies a conservative OWASP baseline of response headers. Content-Security-Policy stays
/// configurable because each UI surface has different asset needs.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IOptions<ServiceDefaultsOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SecurityHeaderOptions headers = options.Value.SecurityHeaders;

        if (headers.Enabled)
        {
            context.Response.OnStarting(() =>
            {
                IHeaderDictionary responseHeaders = context.Response.Headers;

                responseHeaders["X-Content-Type-Options"] = "nosniff";
                responseHeaders["X-Frame-Options"] = "DENY";
                responseHeaders["Referrer-Policy"] = headers.ReferrerPolicy;
                responseHeaders["Permissions-Policy"] = headers.PermissionsPolicy;
                responseHeaders["Content-Security-Policy"] = headers.ContentSecurityPolicy;
                responseHeaders["Strict-Transport-Security"] =
                    $"max-age={TimeSpan.FromDays(headers.StrictTransportSecurityMaxAgeDays).TotalSeconds}; includeSubDomains";

                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
