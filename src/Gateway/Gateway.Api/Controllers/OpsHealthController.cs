using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Models;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("ops/api/v1/health")]
[TenantIndependent]
public sealed class OpsHealthController(
    IHttpClientFactory httpClientFactory,
    IProxyConfigProvider proxyConfigProvider) : ApiControllerBase
{
    [HttpGet("services")]
    [HasPermission(FrameworkPermissions.OpsHealthRead)]
    public async Task<IActionResult> Services(CancellationToken cancellationToken)
    {
        IProxyConfig config = proxyConfigProvider.GetConfig();
        using HttpClient client = httpClientFactory.CreateClient("gateway-health");
        client.Timeout = TimeSpan.FromSeconds(3);

        var results = new List<ServiceHealthResponse>();

        foreach (ClusterConfig cluster in config.Clusters)
        {
            string? address = cluster.Destinations?.Values.Select(destination => destination.Address).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(address))
            {
                results.Add(new ServiceHealthResponse(cluster.ClusterId, "Unknown", "No destination configured", null, false));
                continue;
            }

            string readyUrl = Combine(address, "health/ready");

            try
            {
                using HttpResponseMessage response = await client.GetAsync(readyUrl, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                string status = TryReadStatus(body) ?? (response.IsSuccessStatusCode ? "Healthy" : "Unhealthy");

                results.Add(new ServiceHealthResponse(
                    cluster.ClusterId,
                    status,
                    response.ReasonPhrase,
                    null,
                    response.IsSuccessStatusCode || (int)response.StatusCode is >= 500 and < 600));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                results.Add(new ServiceHealthResponse(cluster.ClusterId, "Unreachable", ex.Message, null, false));
            }
        }

        var aggregate = new HealthAggregateResponse(DateTimeOffset.UtcNow, results);
        return Ok(ApiResponse<HealthAggregateResponse>.Success(aggregate, HttpContext.TraceIdentifier));
    }

    private static string Combine(string baseAddress, string relative)
    {
        if (!baseAddress.EndsWith('/'))
        {
            baseAddress += "/";
        }

        return new Uri(new Uri(baseAddress), relative).ToString();
    }

    private static string? TryReadStatus(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("status", out JsonElement status))
            {
                return null;
            }

            // ASP.NET health JSON may serialize HealthStatus as string or as enum number.
            return status.ValueKind switch
            {
                JsonValueKind.String => status.GetString(),
                JsonValueKind.Number when status.TryGetInt32(out int code) => code switch
                {
                    0 => "Unhealthy",
                    1 => "Degraded",
                    2 => "Healthy",
                    _ => code.ToString()
                },
                _ => status.ToString()
            };
        }
        catch (JsonException)
        {
            // ignore non-JSON health payloads
        }

        return null;
    }
}

public sealed record ServiceHealthResponse(
    string Service,
    string Status,
    string? Description,
    double? DurationMs,
    bool Reachable);

public sealed record HealthAggregateResponse(DateTimeOffset CheckedAtUtc, IReadOnlyList<ServiceHealthResponse> Services);
