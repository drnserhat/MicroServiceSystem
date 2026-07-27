using Microsoft.AspNetCore.Http;
using MicroServiceSystem.SharedKernel.Constants;
using Serilog.Core;
using Serilog.Events;

namespace MicroServiceSystem.BuildingBlocks.Logging.Enrichers;

public sealed class UserEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public const string PropertyName = "UserId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        string? userId = httpContextAccessor.HttpContext?.User.FindFirst(FrameworkClaimTypes.UserId)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, userId));
    }
}
