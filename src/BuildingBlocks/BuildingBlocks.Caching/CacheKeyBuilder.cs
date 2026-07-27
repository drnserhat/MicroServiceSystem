using System.Text;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Caching.Abstractions;
using MicroServiceSystem.BuildingBlocks.Caching.Configuration;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Caching;

public sealed class CacheKeyBuilder(IOptions<CacheOptions> options, ICurrentTenant currentTenant) : ICacheKeyBuilder
{
    private const char Separator = ':';
    private const string GlobalTenantSegment = "global";

    public string Build(string category, params ReadOnlySpan<string> segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var builder = new StringBuilder();

        string instanceName = options.Value.InstanceName;

        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            builder.Append(instanceName).Append(Separator);
        }

        builder
            .Append(currentTenant.Id?.ToString() ?? GlobalTenantSegment)
            .Append(Separator)
            .Append(category);

        foreach (string segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment))
            {
                builder.Append(Separator).Append(segment);
            }
        }

        return builder.ToString();
    }
}
