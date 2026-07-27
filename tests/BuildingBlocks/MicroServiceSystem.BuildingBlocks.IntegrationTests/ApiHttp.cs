using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Deserialization shape for production ApiResponse. The real type uses a private constructor, so
/// tests bind to a mutable DTO instead of fighting the serializer.
/// </summary>
internal sealed class ApiEnvelope<T>
{
    public bool Succeeded { get; set; }

    public T? Data { get; set; }

    public ApiEnvelopeError? Error { get; set; }
}

internal sealed class ApiEnvelopeError
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

internal static class ApiHttp
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Task<ApiEnvelope<T>?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, cancellationToken);
}
