extern alias CoordinatorApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coordinator.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.Testing.Utilities;
using NSubstitute;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// HTTP-level coverage for Coordinator registration's catalog gate and auth boundary. Identity is
/// mocked so this stays a single-host test.
/// </summary>
[Collection(nameof(ApiHostCollection))]
public sealed class CoordinatorApiTests(ApiHostFixture fixture)
{
    [Fact]
    public async Task Registration_without_bearer_token_is_rejected()
    {
        fixture.EnsureAvailable();

        IIdentityServiceClient identity = Substitute.For<IIdentityServiceClient>();
        await using CoordinatorApiFactory factory = CreateFactory(identity);
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/registration",
            NewRegistrationBody(Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registration_rejects_an_unknown_tenant_before_starting_a_saga()
    {
        fixture.EnsureAvailable();

        Guid tenantId = Guid.CreateVersion7();
        IIdentityServiceClient identity = Substitute.For<IIdentityServiceClient>();
        identity.GetTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantCatalogResult?>(null));

        await using CoordinatorApiFactory factory = CreateFactory(identity);
        HttpClient client = factory.CreateClient();
        AttachRegistrarToken(client, tenantId);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/registration",
            NewRegistrationBody(tenantId),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            response,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Succeeded.ShouldBeFalse();
        body.Error!.Code.ShouldBe("coordinator.tenant_not_found");

        await identity.DidNotReceive().RegisterAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Registration_rejects_an_inactive_tenant()
    {
        fixture.EnsureAvailable();

        Guid tenantId = Guid.CreateVersion7();
        IIdentityServiceClient identity = Substitute.For<IIdentityServiceClient>();
        identity.GetTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantCatalogResult?>(
                new TenantCatalogResult(tenantId, "Frozen", "frozen", IsActive: false)));

        await using CoordinatorApiFactory factory = CreateFactory(identity);
        HttpClient client = factory.CreateClient();
        AttachRegistrarToken(client, tenantId);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/registration",
            NewRegistrationBody(tenantId),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            response,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Error!.Code.ShouldBe("coordinator.tenant_inactive");
    }

    [Fact]
    public async Task Registration_rejects_tenant_id_that_does_not_match_the_caller()
    {
        fixture.EnsureAvailable();

        Guid callerTenant = Guid.CreateVersion7();
        Guid otherTenant = Guid.CreateVersion7();
        IIdentityServiceClient identity = Substitute.For<IIdentityServiceClient>();

        await using CoordinatorApiFactory factory = CreateFactory(identity);
        HttpClient client = factory.CreateClient();
        AttachRegistrarToken(client, callerTenant);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/registration",
            NewRegistrationBody(otherTenant),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            response,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Error!.Code.ShouldBe("coordinator.tenant_scope_mismatch");

        await identity.DidNotReceive().GetTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static object NewRegistrationBody(Guid tenantId) =>
        new
        {
            email = "user@example.com",
            userName = "demo.user",
            password = "Str0ng!Pass",
            firstName = "Demo",
            lastName = "User",
            displayName = (string?)null,
            tenantId
        };

    private static void AttachRegistrarToken(HttpClient client, Guid tenantId)
    {
        string token = TestJwtFactory.CreateToken(
            userId: Guid.CreateVersion7(),
            tenantId: tenantId,
            roles: [FrameworkPermissions.AdminRoleName],
            permissions: [FrameworkPermissions.RegistrationUsersCreate]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private CoordinatorApiFactory CreateFactory(IIdentityServiceClient identity)
    {
        var factory = new CoordinatorApiFactory();
        factory.WithSetting("Persistence:Postgres:ConnectionString", fixture.ConnectionString);
        factory.WithSetting("Persistence:Postgres:ApplyMigrationsOnStartup", "true");
        factory.WithSetting("Persistence:Postgres:Schema", "coordinator");
        factory.WithTestServices(services =>
        {
            services.RemoveAll<IIdentityServiceClient>();
            services.AddSingleton(identity);
        });
        return factory;
    }

    private sealed class CoordinatorApiFactory : FrameworkWebApplicationFactory<CoordinatorApi::Program>;
}
