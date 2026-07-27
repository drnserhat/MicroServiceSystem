using MicroServiceSystem.SharedKernel.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace MicroServiceSystem.BuildingBlocks.Logging.Enrichers;

/// <summary>
/// Adds the ambient tenant to every log event so operators can filter a noisy shared log stream down
/// to a single customer.
/// </summary>
public sealed class TenantEnricher(IServiceProvider serviceProvider) : ILogEventEnricher
{
    public const string PropertyName = "TenantId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        if (serviceProvider.GetService(typeof(ICurrentTenant)) is not ICurrentTenant currentTenant
            || currentTenant.Id is not { } tenantId)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, tenantId));
    }
}
