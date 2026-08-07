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
/// Covers admin role assign/unassign including last-Admin protection and permission gates.
/// </summary>
[Collection(nameof(ApiHostCollection))]
public sealed class IdentityUserRoleAdminApiTests(ApiHostFixture fixture)
{
    [Fact]
    public async Task Assign_and_unassign_role_roundtrip_for_authorized_caller()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        ProvisionedUser target = await ProvisionUserAsync(client, "roles.roundtrip@example.com", "rolesround");
        Guid memberRoleId = await EnsureRolesAndGetIdAsync(client, target.TenantId, FrameworkPermissions.MemberRoleName);

        AttachAssignToken(client, target.TenantId);

        using (HttpResponseMessage assign = await client.PostAsync(
                   $"/api/v1/users/{target.UserId:D}/roles/{memberRoleId:D}",
                   content: null,
                   TestContext.Current.CancellationToken))
        {
            // Member is already assigned at register; assign is idempotent success.
            assign.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        Guid adminRoleId = await EnsureRolesAndGetIdAsync(client, target.TenantId, FrameworkPermissions.AdminRoleName);
        AttachAssignToken(client, target.TenantId);

        using (HttpResponseMessage assignAdmin = await client.PostAsync(
                   $"/api/v1/users/{target.UserId:D}/roles/{adminRoleId:D}",
                   content: null,
                   TestContext.Current.CancellationToken))
        {
            assignAdmin.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        ProvisionedUser secondAdmin = await ProvisionUserAsync(client, "roles.second@example.com", "rolessecond", target.TenantId);
        AttachAssignToken(client, target.TenantId);

        (await client.PostAsync(
            $"/api/v1/users/{secondAdmin.UserId:D}/roles/{adminRoleId:D}",
            content: null,
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        using HttpResponseMessage unassign = await client.DeleteAsync(
            $"/api/v1/users/{target.UserId:D}/roles/{adminRoleId:D}",
            TestContext.Current.CancellationToken);

        unassign.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unassign_rejects_removing_admin_from_the_last_active_admin()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        ProvisionedUser admin = await ProvisionUserAsync(client, "last.admin@example.com", "lastadmin");
        Guid adminRoleId = await EnsureRolesAndGetIdAsync(client, admin.TenantId, FrameworkPermissions.AdminRoleName);

        AttachAssignToken(client, admin.TenantId);

        (await client.PostAsync(
            $"/api/v1/users/{admin.UserId:D}/roles/{adminRoleId:D}",
            content: null,
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        using HttpResponseMessage unassign = await client.DeleteAsync(
            $"/api/v1/users/{admin.UserId:D}/roles/{adminRoleId:D}",
            TestContext.Current.CancellationToken);

        unassign.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        ApiEnvelope<object?>? body = await ApiHttp.ReadAsync<object?>(
            unassign,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Succeeded.ShouldBeFalse();
        body.Error!.Code.ShouldBe("identity.last_admin_protected");
    }

    [Fact]
    public async Task Assign_role_without_permission_is_forbidden()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        ProvisionedUser target = await ProvisionUserAsync(client, "roles.denied@example.com", "rolesdenied");
        Guid adminRoleId = await EnsureRolesAndGetIdAsync(client, target.TenantId, FrameworkPermissions.AdminRoleName);

        string token = TestJwtFactory.CreateToken(
            userId: Guid.CreateVersion7(),
            tenantId: target.TenantId,
            roles: [FrameworkPermissions.MemberRoleName],
            permissions: [FrameworkPermissions.IdentityUsersRead]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage assign = await client.PostAsync(
            $"/api/v1/users/{target.UserId:D}/roles/{adminRoleId:D}",
            content: null,
            TestContext.Current.CancellationToken);

        assign.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_error_description_is_localized_for_accept_language()
    {
        fixture.EnsureAvailable();
        await using IdentityApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();
        AttachInternalApiKey(client);

        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        (await client.PostAsJsonAsync(
            "/api/v1/tenants",
            new { name = "Locale Tenant", slug = $"loc-{tenantId:N}"[..16], tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                userId,
                email = "locale.user@example.com",
                userName = "localeuser",
                password = "Str0ng!Pass",
                tenantId
            },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR");

        using HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "locale.user@example.com",
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
        body.Error.Description.ShouldBe("Geçersiz e-posta veya şifre.");
    }

    private static async Task<ProvisionedUser> ProvisionUserAsync(
        HttpClient client,
        string email,
        string userName,
        Guid? existingTenantId = null)
    {
        AttachInternalApiKey(client);

        Guid tenantId = existingTenantId ?? Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        if (existingTenantId is null)
        {
            (await client.PostAsJsonAsync(
                "/api/v1/tenants",
                new { name = $"Role Tenant {tenantId:N}"[..24], slug = $"rt-{tenantId:N}"[..16], tenantId },
                TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        }

        (await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { userId, email, userName, password = "Str0ng!Pass", tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");

        // Login materializes Member + Admin role catalogs for the tenant.
        (await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Str0ng!Pass", tenantId },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        return new ProvisionedUser(userId, tenantId);
    }

    private static async Task<Guid> EnsureRolesAndGetIdAsync(HttpClient client, Guid tenantId, string roleName)
    {
        AttachRolesReadToken(client, tenantId);

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/roles",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        ApiEnvelope<RolePayload[]>? body = await ApiHttp.ReadAsync<RolePayload[]>(
            response,
            TestContext.Current.CancellationToken);

        body.ShouldNotBeNull();
        body!.Succeeded.ShouldBeTrue();
        RolePayload? role = body.Data!.SingleOrDefault(item =>
            string.Equals(item.Name, roleName, StringComparison.OrdinalIgnoreCase));
        role.ShouldNotBeNull();
        return role!.Id;
    }

    private static void AttachAssignToken(HttpClient client, Guid tenantId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                roles: [FrameworkPermissions.AdminRoleName],
                permissions:
                [
                    FrameworkPermissions.IdentityRolesAssign,
                    FrameworkPermissions.IdentityRolesRead
                ]));
    }

    private static void AttachRolesReadToken(HttpClient client, Guid tenantId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtFactory.CreateToken(
                userId: Guid.CreateVersion7(),
                tenantId: tenantId,
                roles: [FrameworkPermissions.AdminRoleName],
                permissions: [FrameworkPermissions.IdentityRolesRead]));
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

    private sealed record ProvisionedUser(Guid UserId, Guid TenantId);

    private sealed record RolePayload(Guid Id, string Name, string[] Permissions);
}
