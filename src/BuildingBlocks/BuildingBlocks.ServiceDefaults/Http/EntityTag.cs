using Microsoft.AspNetCore.Http;

namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults.Http;

/// <summary>
/// Formats and parses strong entity tags for PostgreSQL <c>xmin</c> concurrency tokens.
/// </summary>
public static class EntityTag
{
    public static string Format(uint version) => $"\"{version}\"";

    public static bool TryParse(string? headerValue, out uint version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        ReadOnlySpan<char> span = headerValue.AsSpan().Trim();

        if (span.Equals("*", StringComparison.Ordinal))
        {
            return false;
        }

        if (span.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            span = span[2..].TrimStart();
        }

        if (span.Length >= 2 && span[0] == '"' && span[^1] == '"')
        {
            span = span[1..^1];
        }

        return uint.TryParse(span, out version);
    }

    public static bool TryGetIfMatch(HttpRequest request, out uint version)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (string? value in request.Headers.IfMatch)
        {
            if (TryParse(value, out version))
            {
                return true;
            }
        }

        version = default;
        return false;
    }

    public static void Set(HttpResponse response, uint version)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers.ETag = Format(version);
    }
}
