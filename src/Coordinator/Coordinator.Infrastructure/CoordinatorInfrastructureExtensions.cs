using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Coordinator.Application;
using Coordinator.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Extensions;
using MicroServiceSystem.BuildingBlocks.Resilience.Extensions;
using MicroServiceSystem.BuildingBlocks.Saga;
using MicroServiceSystem.SharedKernel.Models;

namespace Coordinator.Infrastructure;

public static class CoordinatorInfrastructureExtensions
{
    public static IServiceCollection AddCoordinatorInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFrameworkMessaging(configuration, "coordinator", CoordinatorApplicationExtensions.ApplicationAssembly);
        services.AddOutboxProcessor();
        services.AddFrameworkSaga();
        services.Configure<SagaOptions>(configuration.GetSection(SagaOptions.SectionName));
        services.AddHostedService<RegisterUserSagaRecoveryService>();

        services.Configure<IdentityServiceOptions>(configuration.GetSection(IdentityServiceOptions.SectionName));
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<InternalServiceOptions>(configuration.GetSection(InternalServiceOptions.SectionName));

        services.AddFrameworkHttpClient<IIdentityServiceClient, IdentityServiceClient>(configuration)
            .ConfigureHttpClient((sp, client) =>
            {
                IdentityServiceOptions options = sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                AttachInternalApiKey(client, sp);
            });

        services.AddFrameworkHttpClient<IUserServiceClient, UserServiceClient>(configuration)
            .ConfigureHttpClient((sp, client) =>
            {
                UserServiceOptions options = sp.GetRequiredService<IOptions<UserServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                AttachInternalApiKey(client, sp);
            });

        return services;
    }

    private static void AttachInternalApiKey(HttpClient client, IServiceProvider sp)
    {
        InternalServiceOptions options = sp.GetRequiredService<IOptions<InternalServiceOptions>>().Value;

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return;
        }

        client.DefaultRequestHeaders.Remove(options.HeaderName);
        client.DefaultRequestHeaders.TryAddWithoutValidation(options.HeaderName, options.ApiKey);
    }
}

public sealed class IdentityServiceOptions
{
    public const string SectionName = "Services:Identity";

    public string BaseUrl { get; set; } = "http://localhost:5080/";
}

public sealed class UserServiceOptions
{
    public const string SectionName = "Services:User";

    public string BaseUrl { get; set; } = "http://localhost:5081/";
}

public sealed class IdentityServiceClient(HttpClient httpClient) : IIdentityServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<TenantCatalogResult?> GetTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            $"api/v1/tenants/{tenantId:D}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        ApiResponse<TenantPayload>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<TenantPayload>>(
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode || payload is null || !payload.Succeeded || payload.Data is null)
        {
            string detail = payload?.Error?.Description ?? response.ReasonPhrase ?? "Tenant lookup failed.";
            throw new InvalidOperationException(detail);
        }

        return new TenantCatalogResult(
            payload.Data.Id,
            payload.Data.Name,
            payload.Data.Slug,
            payload.Data.IsActive);
    }

    public async Task<IdentityRegistrationResult> RegisterAsync(
        Guid userId,
        string email,
        string userName,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "api/v1/auth/register",
            new { userId, email, userName, password, tenantId },
            cancellationToken);

        ApiResponse<RegisterIdentityPayload>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterIdentityPayload>>(
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode || payload is null || !payload.Succeeded || payload.Data is null)
        {
            string detail = payload?.Error?.Description ?? response.ReasonPhrase ?? "Identity registration failed.";
            throw new InvalidOperationException(detail);
        }

        return new IdentityRegistrationResult(payload.Data.UserId, payload.Data.Email, payload.Data.UserName);
    }

    public async Task DisableAsync(Guid userId, string reason, Guid tenantId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "api/v1/auth/disable",
            new { userId, reason, tenantId },
            cancellationToken);

        // Compensation runs against an id the saga reserved before calling register, so the user may never
        // have been created. Nothing to undo is a successful undo.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Identity disable failed with status {(int)response.StatusCode}.");
        }
    }

    private sealed record RegisterIdentityPayload(Guid UserId, string Email, string UserName);

    private sealed record TenantPayload(Guid Id, string Name, string Slug, bool IsActive);
}

public sealed class UserServiceClient(HttpClient httpClient) : IUserServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<UserProfileResult> CreateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? displayName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "api/v1/users/profiles",
            new { userId, firstName, lastName, displayName, tenantId },
            cancellationToken);

        ApiResponse<UserProfilePayload>? payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfilePayload>>(
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode || payload is null || !payload.Succeeded || payload.Data is null)
        {
            string detail = payload?.Error?.Description ?? response.ReasonPhrase ?? "User profile creation failed.";
            throw new InvalidOperationException(detail);
        }

        return new UserProfileResult(
            payload.Data.Id,
            payload.Data.FirstName,
            payload.Data.LastName,
            payload.Data.DisplayName,
            payload.Data.IsActive);
    }

    private sealed record UserProfilePayload(
        Guid Id,
        string FirstName,
        string LastName,
        string DisplayName,
        bool IsActive);
}
