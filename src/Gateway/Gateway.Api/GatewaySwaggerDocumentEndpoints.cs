using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Gateway.Api;

internal static class GatewaySwaggerDocumentEndpoints
{
    private static readonly string[] Services =
    [
        "identity",
        "user",
        "coordinator",
        "notification",
        "file",
        "audit",
        "settings",
        "location",
        "logging"
    ];

    public static IEndpointRouteBuilder MapGatewaySwaggerDocuments(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/docs/{service}/swagger.json",
                async (
                    string service,
                    IConfiguration configuration,
                    IHttpClientFactory httpClientFactory,
                    CancellationToken cancellationToken) =>
                {
                    string key = service.Trim().ToLowerInvariant();
                    if (Array.IndexOf(Services, key) < 0)
                    {
                        return Results.NotFound();
                    }

                    string? baseAddress = configuration[$"ReverseProxy:Clusters:{key}:Destinations:d1:Address"];
                    if (string.IsNullOrWhiteSpace(baseAddress))
                    {
                        return Results.NotFound();
                    }

                    HttpClient client = httpClientFactory.CreateClient("gateway-swagger");
                    Uri swaggerUri = new(new Uri(baseAddress, UriKind.Absolute), "swagger/v1/swagger.json");

                    using HttpResponseMessage response = await client.GetAsync(swaggerUri, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        return Results.StatusCode((int)response.StatusCode);
                    }

                    await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using JsonDocument document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

                    using var buffer = new MemoryStream();
                    await using (var writer = new Utf8JsonWriter(buffer))
                    {
                        writer.WriteStartObject();

                        foreach (JsonProperty property in document.RootElement.EnumerateObject())
                        {
                            if (property.NameEquals("servers"))
                            {
                                continue;
                            }

                            property.WriteTo(writer);
                        }

                        // Point Try-it-out at the gateway prefix so YARP routes are hit.
                        writer.WritePropertyName("servers");
                        writer.WriteStartArray();
                        writer.WriteStartObject();
                        writer.WriteString("url", $"/{key}");
                        writer.WriteEndObject();
                        writer.WriteEndArray();

                        writer.WriteEndObject();
                    }

                    return Results.Bytes(buffer.ToArray(), "application/json");
                })
            .AllowAnonymous()
            .WithDisplayName("Gateway rewritten OpenAPI documents");

        return endpoints;
    }
}
