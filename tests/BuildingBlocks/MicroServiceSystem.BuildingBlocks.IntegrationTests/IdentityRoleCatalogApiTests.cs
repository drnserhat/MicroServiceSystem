extern alias IdentityApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Security;
using MicroServiceSystem.Testing.Utilities;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

/// <summary>
/// Custom role catalog CRUD: create / replace / delete with built-in and allowlist guards.
/// </summary>
[Collection(nameof(ApiHostCollection))]
public sealed class IdentityRoleCatalogApiTests(ApiHostFixture fixture)
{
    [Fact]
    public async Task Create_replace_and_delete_custom_role_roundtrip()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        Guid tenantId = await ProvisionTenantAsync(client);
        AttachWriteToken(client, tenantId);

        using (HttpResponseMessage create = await client.PostAsJsonAsync(
                   "/api/v1/roles",
                   new
                   {
                       name = "Support",
                       permissions = new[] { FrameworkPermissions.UsersProfilesRead }
                   },
                   TestContext.Current.CancellationToken))
        {
            create.StatusCode.ShouldBe(HttpStatusCode.Created);

            ApiEnvelope<RolePayload>? created = await ApiHttp.ReadAsync<RolePayload>(
                create,
                TestContext.Current.CancellationToken);

            created.ShouldNotBeNull();
            created!.Succeeded.ShouldBeTrue();
            created.Data!.Name.ShouldBe("Support");
            created.Data.Permissions.ShouldBe([FrameworkPermissions.UsersProfilesRead]);

            Guid roleId = created.Data.Id;

            using (HttpResponseMessage replace = await client.PutAsJsonAsync(
                       $"/api/v1/roles/{roleId:D}",
                       new
                       {
                           name = "SupportDesk",
                           permissions = new[]
                           {
                               FrameworkPermissions.UsersProfilesRead,
                               FrameworkPermissions.AuditEntriesRead
                           }
                       },
                       TestContext.Current.CancellationToken))
            {
                replace.StatusCode.ShouldBe(HttpStatusCode.OK);

                ApiEnvelope<RolePayload>? replaced = await ApiHttp.ReadAsync<RolePayload>(
                    replace,
                    TestContext.Current.CancellationToken);

                replaced!.Data!.Name.ShouldBe("SupportDesk");
                replaced.Data.Permissions.Length.ShouldBe(2);
            }

            using HttpResponseMessage delete = await client.DeleteAsync(
                $"/api/v1/roles/{roleId:D}",
                TestContext.Current.CancellationToken);

            delete.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Delete_fails_when_role_is_still_assigned()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        (Guid tenantId, Guid userId) = await ProvisionUserAsync(client, "role.inuse@example.com", "roleinuse");
        AttachWriteToken(client, tenantId);

        ApiEnvelope<RolePayload>? created = await CreateRoleAsync(client, "TempAssigned", [FrameworkPermissions.UsersProfilesRead]);
        Guid roleId = created!.Data!.Id;

        AttachAssignToken(client, tenantId);
        (await client.PostAsync(
            $"/api/v1/users/{userId:D}/roles/{roleId:D}",
            content: null,
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        AttachWriteToken(client, tenantId);
        using HttpResponseMessage delete = await client.DeleteAsync(
            $"/api/v1/roles/{roleId:D}",
            TestContext.Current.CancellationToken);

        delete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(delete, TestContext.Current.CancellationToken);
        body!.Error!.Code.ShouldBe("identity.role_in_use");
    }

    [Fact]
    public async Task Built_in_admin_role_cannot_be_replaced_or_deleted()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        (Guid tenantId, _) = await ProvisionUserAsync(client, "role.builtin@example.com", "rolebuiltin");
        Guid adminRoleId = await GetRoleIdAsync(client, tenantId, FrameworkPermissions.AdminRoleName);

        AttachWriteToken(client, tenantId);

        using (HttpResponseMessage replace = await client.PutAsJsonAsync(
                   $"/api/v1/roles/{adminRoleId:D}",
                   new { name = "NotAdmin", permissions = Array.Empty<string>() },
                   TestContext.Current.CancellationToken))
        {
            replace.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await ApiHttp.ReadAsync<object?>(replace, TestContext.Current.CancellationToken))!
                .Error!.Code.ShouldBe("identity.built_in_role_protected");
        }

        using HttpResponseMessage delete = await client.DeleteAsync(
            $"/api/v1/roles/{adminRoleId:D}",
            TestContext.Current.CancellationToken);

        delete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ApiHttp.ReadAsync<object?>(delete, TestContext.Current.CancellationToken))!
            .Error!.Code.ShouldBe("identity.built_in_role_protected");
    }

    [Fact]
    public async Task Create_rejects_reserved_name_and_unknown_permission()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        Guid tenantId = await ProvisionTenantAsync(client);
        AttachWriteToken(client, tenantId);

        using (HttpResponseMessage reserved = await client.PostAsJsonAsync(
                   "/api/v1/roles",
                   new { name = "Admin", permissions = Array.Empty<string>() },
                   TestContext.Current.CancellationToken))
        {
            reserved.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await ApiHttp.ReadAsync<object?>(reserved, TestContext.Current.CancellationToken))!
                .Error!.Code.ShouldBe("identity.role_name_reserved");
        }

        using HttpResponseMessage unknown = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new { name = "Custom", permissions = new[] { "not.a.real.permission" } },
            TestContext.Current.CancellationToken);

        unknown.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ApiHttp.ReadAsync<object?>(unknown, TestContext.Current.CancellationToken))!
            .Error!.Code.ShouldBe("identity.permission_unknown");
    }

    [Fact]
    public async Task Create_without_write_permission_is_forbidden()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        Guid tenantId = await ProvisionTenantAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                permissions: [FrameworkPermissions.IdentityRolesRead]));

        using HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new { name = "Denied", permissions = Array.Empty<string>() },
            TestContext.Current.CancellationToken);

        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<ApiEnvelope<RolePayload>?> CreateRoleAsync(
        HttpClient client,
        string name,
        string[] permissions)
    {
        using HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new { name, permissions },
            TestContext.Current.CancellationToken);

        create.EnsureSuccessStatusCode();
        return await ApiHttp.ReadAsync<RolePayload>(create, TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> GetRoleIdAsync(HttpClient client, Guid tenantId, string roleName)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                permissions: [FrameworkPermissions.IdentityRolesRead]));

        using HttpResponseMessage response = await client.GetAsync("/api/v1/roles", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        ApiEnvelope<RolePayload[]>? body = await ApiHttp.ReadAsync<RolePayload[]>(
            response,
            TestContext.Current.CancellationToken);

        return body!.Data!.Single(role =>
            string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase)).Id;
    }

    private static async Task<Guid> ProvisionTenantAsync(HttpClient client)
    {
        AttachInternalApiKey(client);
        Guid tenantId = Guid.CreateVersion7();

        (await client.PostAsJsonAsync(
            "/api/v1/tenants",
            new { name = $"CRUD Tenant {tenantId:N}"[..24], slug = $"rc-{tenantId:N}"[..16], tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
        return tenantId;
    }

    private static async Task<(Guid TenantId, Guid UserId)> ProvisionUserAsync(
        HttpClient client,
        string email,
        string userName)
    {
        AttachInternalApiKey(client);
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        (await client.PostAsJsonAsync(
            "/api/v1/tenants",
            new { name = $"CRUD User Tenant {tenantId:N}"[..28], slug = $"ru-{tenantId:N}"[..16], tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { userId, email, userName, password = "Str0ng!Pass", tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");

        (await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Str0ng!Pass", tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        return (tenantId, userId);
    }

    private static void AttachWriteToken(HttpClient client, Guid tenantId) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                roles: [FrameworkPermissions.AdminRoleName],
                permissions:
                [
                    FrameworkPermissions.IdentityRolesWrite,
                    FrameworkPermissions.IdentityRolesRead
                ]));

    private static void AttachAssignToken(HttpClient client, Guid tenantId) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                permissions:
                [
                    FrameworkPermissions.IdentityRolesAssign,
                    FrameworkPermissions.IdentityRolesRead
                ]));

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

    private sealed record RolePayload(Guid Id, string Name, string[] Permissions);
}
