using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Idempotency.Abstractions;
using MicroServiceSystem.BuildingBlocks.Idempotency.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Idempotency;

/// <summary>
/// Cache backed idempotency store. A reserved key blocks concurrent replays; a stored response lets a
/// client safely retry a mutation that already succeeded.
/// </summary>
public sealed class DistributedCacheIdempotencyStore(IDistributedCache cache) : IIdempotencyStore
{
    private const string ReservedMarker = "__reserved__";
    private const string KeyPrefix = "idempotency:";

    public async Task<bool> TryReserveAsync(string key, TimeSpan retention, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[]? existing = await cache.GetAsync(BuildKey(key), cancellationToken);

        if (existing is not null)
        {
            return false;
        }

        await cache.SetStringAsync(
            BuildKey(key),
            ReservedMarker,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = retention },
            cancellationToken);

        return true;
    }

    public async Task<string?> GetResponseAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string? value = await cache.GetStringAsync(BuildKey(key), cancellationToken);

        return value is null or ReservedMarker ? null : value;
    }

    public Task StoreResponseAsync(
        string key,
        string response,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(response);

        return cache.SetStringAsync(
            BuildKey(key),
            response,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = retention },
            cancellationToken);
    }

    public Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return cache.RemoveAsync(BuildKey(key), cancellationToken);
    }

    private static string BuildKey(string key) => $"{KeyPrefix}{key}";
}

/// <summary>
/// Captures successful mutation responses so a retry with the same idempotency key returns the original
/// payload instead of creating a duplicate side effect.
/// </summary>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IIdempotencyStore store,
    IOptions<IdempotencyOptions> options)
{
    private static readonly HashSet<string> MutationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IdempotencyOptions idempotencyOptions = options.Value;

        if (!idempotencyOptions.Enabled || !MutationMethods.Contains(context.Request.Method))
        {
            await next(context);

            return;
        }

        if (!context.Request.Headers.TryGetValue(idempotencyOptions.HeaderName, out var header)
            || string.IsNullOrWhiteSpace(header))
        {
            if (idempotencyOptions.RequireKeyForMutations)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Idempotency key is required." });

                return;
            }

            await next(context);

            return;
        }

        string key = header.ToString();
        TimeSpan retention = TimeSpan.FromHours(idempotencyOptions.RetentionHours);

        string? cachedResponse = await store.GetResponseAsync(key, context.RequestAborted);

        if (cachedResponse is not null)
        {
            CachedIdempotentResponse? replay = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedResponse);

            if (replay is not null)
            {
                context.Response.StatusCode = replay.StatusCode;
                context.Response.ContentType = replay.ContentType;

                await context.Response.WriteAsync(replay.Body);

                return;
            }
        }

        if (!await store.TryReserveAsync(key, retention, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = "A request with this idempotency key is already in progress." });

            return;
        }

        Stream originalBody = context.Response.Body;

        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                buffer.Position = 0;
                string body = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

                var cached = new CachedIdempotentResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    body);

                await store.StoreResponseAsync(
                    key,
                    JsonSerializer.Serialize(cached),
                    retention,
                    context.RequestAborted);
            }
            else
            {
                await store.ReleaseAsync(key, context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            await store.ReleaseAsync(key, CancellationToken.None);

            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private sealed record CachedIdempotentResponse(int StatusCode, string ContentType, string Body);
}
