using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using MicroServiceSystem.BuildingBlocks.Localization.Abstractions;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Localization;

public sealed class JsonErrorLocalizer : IErrorLocalizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Assembly _assembly = typeof(JsonErrorLocalizer).Assembly;

    public Error Localize(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(error.Code))
        {
            return error;
        }

        string? localized = FindTranslation(error.Code, CultureInfo.CurrentUICulture)
            ?? FindTranslation(error.Code, CultureInfo.CurrentCulture);

        return localized is null ? error : error.WithDescription(localized);
    }

    private string? FindTranslation(string code, CultureInfo culture)
    {
        for (CultureInfo? current = culture; current is not null && !Equals(current, CultureInfo.InvariantCulture); current = current.Parent)
        {
            IReadOnlyDictionary<string, string> map = LoadCulture(current.Name);
            if (map.TryGetValue(code, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (string.IsNullOrEmpty(current.Name))
            {
                break;
            }
        }

        IReadOnlyDictionary<string, string> fallback = LoadCulture("en-US");
        return fallback.TryGetValue(code, out string? english) ? english : null;
    }

    private IReadOnlyDictionary<string, string> LoadCulture(string cultureName)
    {
        return _cache.GetOrAdd(cultureName, static (name, state) => state.ReadEmbedded(name), this);
    }

    private IReadOnlyDictionary<string, string> ReadEmbedded(string cultureName)
    {
        string resourceName = $"{_assembly.GetName().Name}.Resources.Errors.errors.{cultureName}.json";
        using Stream? stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        Dictionary<string, string>? map = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, SerializerOptions);
        return map is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
    }
}
