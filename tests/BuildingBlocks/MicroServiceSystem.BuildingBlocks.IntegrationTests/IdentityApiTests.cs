extern alias IdentityApi;

using System.Net;
using System.Net.Http.Json;
using MicroServiceSystem.SharedKernel.Security;
using MicroServiceSystem.Testing.Utilities;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// HTTP-level coverage for Identity's auth and tenant catalog — the first real use of
/// <see cref="FrameworkWebApplicationFactory{TEntryPoint}"/> in this repo.
/// </summary>
[Collection(nameof(ApiHostCollection))]
public sealed class IdentityApiTests(ApiHostFixture fixture)
{
    [Fact]
    public async Task Register_without_internal_api_key_is_rejected()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                userId = Guid.CreateVersion7(),
                email = "orphan@example.com",
                userName = "orphan",
                password = "Str0ng!Pass",
                tenantId = Guid.CreateVersion7()
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tenant_lookup_without_internal_api_key_is_rejected()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/tenants/{Guid.CreateVersion7():D}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_rejects_an_unknown_tenant()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "nobody@example.com",
                password = "Str0ng!Pass",
                tenantId = Guid.CreateVersion7()
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            response,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Succeeded.ShouldBeFalse();
        body.Error!.Code.ShouldBe("identity.tenant_not_found");
    }

    [Fact]
    public async Task Register_and_login_succeed_for_a_provisioned_tenant()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();
        AttachInternalApiKey(client);

        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        const string email = "demo.user@example.com";
        const string password = "Str0ng!Pass";

        using (HttpResponseMessage createTenant = await client.PostAsJsonAsync(
                   "/api/v1/tenants",
                   new { name = "API Test Tenant", slug = $"api-{tenantId:N}"[..16], tenantId },
                   TestContext.Current.CancellationToken))
        {
            createTenant.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using (HttpResponseMessage register = await client.PostAsJsonAsync(
                   "/api/v1/auth/register",
                   new
                   {
                       userId,
                       email,
                       userName = "demouser",
                       password,
                       tenantId
                   },
                   TestContext.Current.CancellationToken))
        {
            register.StatusCode.ShouldBe(HttpStatusCode.OK);

            ApiEnvelope<RegisterPayload>? registerBody = await ApiHttp.ReadAsync<RegisterPayload>(
                register,
                TestContext.Current.CancellationToken);

            registerBody.ShouldNotBeNull();
            registerBody!.Succeeded.ShouldBeTrue();
            registerBody.Data!.UserId.ShouldBe(userId);
        }

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");

        using HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password, tenantId },
            TestContext.Current.CancellationToken);

        login.StatusCode.ShouldBe(HttpStatusCode.OK);

        ApiEnvelope<LoginPayload>? loginBody = await ApiHttp.ReadAsync<LoginPayload>(
            login,
            TestContext.Current.CancellationToken);

        loginBody.ShouldNotBeNull();
        loginBody!.Succeeded.ShouldBeTrue();
        loginBody.Data!.UserId.ShouldBe(userId);
        loginBody.Data.AccessToken.ShouldNotBeNullOrWhiteSpace();
        loginBody.Data.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_wrong_password_does_not_issue_tokens()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();
        AttachInternalApiKey(client);

        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        (await client.PostAsJsonAsync(
            "/api/v1/tenants",
            new { name = "Wrong Password Tenant", slug = $"wp-{tenantId:N}"[..16], tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                userId,
                email = "wrong.pass@example.com",
                userName = "wrongpass",
                password = "Str0ng!Pass",
                tenantId
            },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");

        using HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "wrong.pass@example.com",
                password = "not-the-password",
                tenantId
            },
            TestContext.Current.CancellationToken);

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            login,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Error!.Code.ShouldBe("identity.invalid_credentials");
    }

    private IdentityApiFactory CreateFactory()
    {
        var factory = new IdentityApiFactory();
        factory.WithSetting("Persistence:Postgres:ConnectionString", fixture.ConnectionString);
        factory.WithSetting("Persistence:Postgres:ApplyMigrationsOnStartup", "true");
        factory.WithSetting("Persistence:Postgres:Schema", "identity");
        return factory;
    }

    private static void AttachInternalApiKey(HttpClient client) =>
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Internal-Api-Key",
            KnownInsecureSecrets.DevelopmentInternalApiKey);

    private sealed class IdentityApiFactory : FrameworkWebApplicationFactory<IdentityApi::Program>;

    private sealed record RegisterPayload(Guid UserId, string Email, string UserName);

    private sealed record LoginPayload(
        Guid UserId,
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAtUtc);
}
