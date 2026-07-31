using System.Reflection;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Configuration;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Configuration;
using MicroServiceSystem.SharedKernel.Security;
using Shouldly;

namespace MicroServiceSystem.Architecture.Tests;

public sealed class SecureByDefaultRulesTests
{
    [Fact]
    public void Known_insecure_development_secrets_are_centralized()
    {
        KnownInsecureSecrets.DevelopmentJwtSigningKey.Length.ShouldBeGreaterThanOrEqualTo(32);
        KnownInsecureSecrets.DevelopmentInternalApiKey.ShouldNotBeNullOrWhiteSpace();
        KnownInsecureSecrets.IsDevelopmentJwtSigningKey(KnownInsecureSecrets.DevelopmentJwtSigningKey)
            .ShouldBeTrue();
        KnownInsecureSecrets.IsDevelopmentInternalApiKey(KnownInsecureSecrets.DevelopmentInternalApiKey)
            .ShouldBeTrue();
    }

    [Fact]
    public void Authentication_assembly_registers_production_secret_validators()
    {
        Type[] types = typeof(JwtOptions).Assembly.GetTypes();

        types.ShouldContain(type => type.Name == "JwtOptionsValidator");
        types.ShouldContain(type => type.Name == "InternalServiceOptionsValidator");
    }

    [Fact]
    public void Multi_tenancy_assembly_registers_production_tenant_validator()
    {
        typeof(MultiTenancyOptions).Assembly.GetTypes()
            .ShouldContain(type => type.Name == "MultiTenancyOptionsValidator");
    }

    [Fact]
    public void Service_defaults_disable_swagger_by_default()
    {
        new ServiceDefaultsOptions().EnableSwagger.ShouldBeFalse();

        typeof(ServiceDefaultsOptions).Assembly.GetTypes()
            .ShouldContain(type => type.Name == "ServiceDefaultsOptionsValidator");
    }

    [Fact]
    public void Outbox_and_inbox_options_expose_lock_duration()
    {
        PropertyInfo? outboxLock = typeof(MicroServiceSystem.BuildingBlocks.Messaging.Configuration.OutboxOptions)
            .GetProperty("LockDurationSeconds");
        PropertyInfo? inboxLock = typeof(MicroServiceSystem.BuildingBlocks.Messaging.Configuration.InboxOptions)
            .GetProperty("LockDurationSeconds");

        outboxLock.ShouldNotBeNull();
        inboxLock.ShouldNotBeNull();
    }

    [Fact]
    public void Production_api_appsettings_leave_secrets_empty()
    {
        string repoRoot = FindRepoRoot();
        string[] apiAppsettings =
        [
            .. Directory.EnumerateFiles(
                Path.Combine(repoRoot, "src"),
                "appsettings.json",
                SearchOption.AllDirectories)
                .Where(IsApiAppsettings)
        ];

        apiAppsettings.ShouldNotBeEmpty();

        foreach (string path in apiAppsettings)
        {
            string json = File.ReadAllText(path);

            json.ShouldNotContain(KnownInsecureSecrets.DevelopmentJwtSigningKey);
            json.ShouldNotContain(KnownInsecureSecrets.DevelopmentInternalApiKey);

            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                         json,
                         "\"(?:SigningKey|ApiKey|Password|ConnectionString)\"\\s*:\\s*\"([^\"]*)\""))
            {
                match.Groups[1].Value.ShouldBeNullOrWhiteSpace(
                    customMessage: $"{path} must not commit a non-empty '{match.Groups[0].Value}'");
            }
        }
    }

    [Fact]
    public void Production_migration_job_exists()
    {
        string repoRoot = FindRepoRoot();

        File.Exists(Path.Combine(repoRoot, "deploy", "migrate", "migrate-all.sh")).ShouldBeTrue();
        File.Exists(Path.Combine(repoRoot, "deploy", "migrate", "migrate-all.ps1")).ShouldBeTrue();
        File.Exists(Path.Combine(repoRoot, "deploy", "secrets", "example.env")).ShouldBeTrue();
        File.Exists(Path.Combine(repoRoot, ".github", "workflows", "ci.yml")).ShouldBeTrue();

        string ci = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));
        ci.ShouldContain("migrate-all.sh");
        ci.ShouldContain("Apply EF migrations");
    }

    [Fact]
    public void Gateway_requires_authentication_by_default_with_anonymous_allowlist()
    {
        string repoRoot = FindRepoRoot();
        string gatewayRoot = Path.Combine(repoRoot, "src", "Gateway", "Gateway.Api");

        string program = File.ReadAllText(Path.Combine(gatewayRoot, "Program.cs"));
        program.ShouldNotContain("MapReverseProxy().AllowAnonymous()");

        string appsettings = File.ReadAllText(Path.Combine(gatewayRoot, "appsettings.json"));
        appsettings.ShouldContain("\"RequireAuthenticatedByDefault\": true");
        appsettings.ShouldContain("identity-auth-login-route");
        appsettings.ShouldContain("identity-auth-refresh-route");
        appsettings.ShouldContain("\"AuthorizationPolicy\": \"Anonymous\"");
        appsettings.ShouldNotContain("coordinator-registration-route");

        // Convenience /registration rewrite must not opt out of the JWT fallback.
        int registrationRoute = appsettings.IndexOf("\"registration-route\"", StringComparison.Ordinal);
        registrationRoute.ShouldBeGreaterThanOrEqualTo(0);
        string registrationSlice = appsettings[registrationRoute..Math.Min(appsettings.Length, registrationRoute + 420)];
        registrationSlice.ShouldNotContain("\"AuthorizationPolicy\": \"Anonymous\"");
    }

    [Fact]
    public void Registration_endpoint_is_not_anonymous()
    {
        string repoRoot = FindRepoRoot();
        string controller = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Coordinator",
            "Coordinator.Api",
            "Controllers",
            "RegistrationController.cs"));

        controller.ShouldNotContain("AllowAnonymous");
        controller.ShouldContain("RegistrationUsersCreate");
    }

    [Fact]
    public void User_profile_exposes_concurrency_version_and_if_match_update()
    {
        string repoRoot = FindRepoRoot();

        File.Exists(Path.Combine(
                repoRoot,
                "src",
                "BuildingBlocks",
                "BuildingBlocks.ServiceDefaults",
                "Http",
                "EntityTag.cs"))
            .ShouldBeTrue();

        string controller = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "User",
            "MicroServiceSystem.Services.User.Api",
            "Controllers",
            "UsersController.cs"));

        controller.ShouldContain("EntityTag.TryGetIfMatch");
        controller.ShouldContain("ToActionResultWithETag");
        controller.ShouldContain("HttpPut(\"profiles/{id:guid}\")");
        controller.ShouldContain("UsersProfilesUpdate");

        string response = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "User",
            "MicroServiceSystem.Services.User.Application",
            "Profiles",
            "Create",
            "CreateUserProfileCommand.cs"));

        response.ShouldContain("uint Version");
    }

    [Fact]
    public void Settings_exposes_list_delete_and_etag_concurrency()
    {
        string repoRoot = FindRepoRoot();

        string controller = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Settings",
            "MicroServiceSystem.Services.Settings.Api",
            "Controllers",
            "SettingsController.cs"));

        controller.ShouldContain("ListSettingsQuery");
        controller.ShouldContain("DeleteSettingCommand");
        controller.ShouldContain("EntityTag.TryGetIfMatch");
        controller.ShouldContain("ToActionResultWithETag");
        controller.ShouldContain("MissingIfMatch");
        controller.ShouldContain("HttpDelete(\"{key}\")");

        string commands = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Settings",
            "MicroServiceSystem.Services.Settings.Application",
            "SettingsCommands.cs"));

        commands.ShouldContain("uint Version");
        commands.ShouldContain("ExpectedVersion");
        commands.ShouldContain("ListSettingsQuery");
        commands.ShouldContain("DeleteSettingCommand");
    }

    [Fact]
    public void Location_countries_expose_get_update_delete_and_etag_concurrency()
    {
        string repoRoot = FindRepoRoot();

        string controller = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Location",
            "MicroServiceSystem.Services.Location.Api",
            "Controllers",
            "CountriesController.cs"));

        controller.ShouldContain("GetCountryByCodeQuery");
        controller.ShouldContain("UpdateCountryCommand");
        controller.ShouldContain("DeleteCountryCommand");
        controller.ShouldContain("EntityTag.TryGetIfMatch");
        controller.ShouldContain("ToActionResultWithETag");
        controller.ShouldContain("LocationCountriesWrite");

        string commands = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Location",
            "MicroServiceSystem.Services.Location.Application",
            "CountryCommands.cs"));

        commands.ShouldContain("uint Version");
        commands.ShouldContain("ExpectedVersion");
    }

    [Fact]
    public void Logging_list_supports_source_time_correlation_and_get_by_id()
    {
        string repoRoot = FindRepoRoot();

        string controller = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Logging",
            "MicroServiceSystem.Services.Logging.Api",
            "Controllers",
            "LogsController.cs"));

        controller.ShouldContain("correlationId");
        controller.ShouldContain("fromUtc");
        controller.ShouldContain("GetSystemLogByIdQuery");
        controller.ShouldContain("HttpGet(\"{id:guid}\")");

        string commands = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Logging",
            "MicroServiceSystem.Services.Logging.Application",
            "IngestSystemLogCommand.cs"));

        commands.ShouldContain("CorrelationId");
        commands.ShouldContain("FromUtc");
        commands.ShouldContain("GetSystemLogByIdQuery");
    }

    [Fact]
    public void RegisterUser_profile_creation_is_saga_owned_not_choreographed()
    {
        string repoRoot = FindRepoRoot();
        string handler = Path.Combine(
            repoRoot,
            "src",
            "Services",
            "User",
            "MicroServiceSystem.Services.User.Application",
            "IntegrationEvents",
            "UserRegisteredIntegrationEventHandler.cs");

        File.Exists(handler).ShouldBeFalse(
            customMessage: "UserRegistered must not create profiles; RegisterUser saga owns that step.");

        string contracts = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Shared",
            "Contracts",
            "Events",
            "Identity",
            "IdentityIntegrationEvents.cs"));

        contracts.ShouldContain("Profile rows are created only by the RegisterUser saga");
    }

    [Fact]
    public void User_lifecycle_integration_events_have_audit_consumers()
    {
        string repoRoot = FindRepoRoot();
        string handlers = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Services",
            "Audit",
            "MicroServiceSystem.Services.Audit.Application",
            "IntegrationEvents",
            "LifecycleAuditIntegrationEventHandlers.cs"));

        handlers.ShouldContain("IIntegrationEventHandler<UserProfileCreatedIntegrationEvent>");
        handlers.ShouldContain("IIntegrationEventHandler<UserRegisteredIntegrationEvent>");
        handlers.ShouldContain("IIntegrationEventHandler<UserDisabledIntegrationEvent>");
        handlers.ShouldContain("IIntegrationEventHandler<UserProfileDeactivatedIntegrationEvent>");
        handlers.ShouldContain("user.profile.created");
        handlers.ShouldContain("identity.user.registered");
    }

    [Fact]
    public void Prometheus_scrapes_every_api_host_and_ci_publishes_images()
    {
        string repoRoot = FindRepoRoot();
        string prometheus = File.ReadAllText(Path.Combine(
            repoRoot,
            "deploy",
            "observability",
            "prometheus",
            "prometheus.yml"));

        string[] jobs =
        [
            "gateway",
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

        foreach (string job in jobs)
        {
            prometheus.ShouldContain($"job_name: {job}");
            prometheus.ShouldContain($"{job}:8080");
        }

        File.Exists(Path.Combine(repoRoot, "deploy", "docker", "docker-compose.observability.yml"))
            .ShouldBeTrue();

        string ci = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "ci.yml"));
        ci.ShouldContain("publish-images");
        ci.ShouldContain("ghcr.io");
        ci.ShouldContain("msf-");
        ci.ShouldContain("docker-compose.observability.yml");
    }

    [Fact]
    public void Helm_chart_deploys_ghcr_images_for_gateway_and_apis()
    {
        string repoRoot = FindRepoRoot();
        string chartDir = Path.Combine(repoRoot, "deploy", "helm", "microservice-system");

        File.Exists(Path.Combine(chartDir, "Chart.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(chartDir, "values.yaml")).ShouldBeTrue();

        string chart = File.ReadAllText(Path.Combine(chartDir, "Chart.yaml"));
        chart.ShouldContain("name: microservice-system");

        string values = File.ReadAllText(Path.Combine(chartDir, "values.yaml"));
        values.ShouldContain("repositoryOwner");
        values.ShouldContain("registry: ghcr.io");
        values.ShouldContain("apps:");
        values.ShouldContain("gateway:");
        values.ShouldContain("identity:");

        string helpers = File.ReadAllText(Path.Combine(chartDir, "templates", "_helpers.tpl"));
        helpers.ShouldContain("msf-%s");

        string gateway = File.ReadAllText(Path.Combine(chartDir, "templates", "gateway.yaml"));
        gateway.ShouldContain("ReverseProxy__Clusters__");
        gateway.ShouldContain("healthPath");

        string valuesHealth = File.ReadAllText(Path.Combine(chartDir, "values.yaml"));
        valuesHealth.ShouldContain("healthPath: /health/live");
        valuesHealth.ShouldContain("healthPath: /health/ready");

        string apps = File.ReadAllText(Path.Combine(chartDir, "templates", "apps.yaml"));
        apps.ShouldContain("ApplyMigrationsOnStartup");
        apps.ShouldContain("Persistence__Postgres__ConnectionString");
        apps.ShouldContain("healthPath");
    }

    [Fact]
    public void Admin_spa_uses_tabler_and_gateway_jwt_paths()
    {
        string repoRoot = FindRepoRoot();
        string adminDir = Path.Combine(repoRoot, "apps", "admin");

        File.Exists(Path.Combine(adminDir, "package.json")).ShouldBeTrue();
        File.Exists(Path.Combine(adminDir, "src", "App.tsx")).ShouldBeTrue();

        string packageJson = File.ReadAllText(Path.Combine(adminDir, "package.json"));
        packageJson.ShouldContain("@tabler/core");
        packageJson.ShouldContain("react-router-dom");

        string main = File.ReadAllText(Path.Combine(adminDir, "src", "main.tsx"));
        main.ShouldContain("@tabler/core");

        string auth = File.ReadAllText(Path.Combine(adminDir, "src", "api", "auth.ts"));
        auth.ShouldContain("/identity/api/v1/auth/login");
        auth.ShouldContain("/identity/api/v1/auth/refresh");

        string settings = File.ReadAllText(Path.Combine(adminDir, "src", "api", "settings.ts"));
        settings.ShouldContain("/settings/api/v1/settings");

        string gatewayDev = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Gateway",
            "Gateway.Api",
            "appsettings.Development.json"));
        gatewayDev.ShouldContain("localhost:5173");

        string appsCompose = File.ReadAllText(Path.Combine(
            repoRoot,
            "deploy",
            "docker",
            "docker-compose.apps.yml"));
        appsCompose.ShouldContain("admin:");
        appsCompose.ShouldContain("apps/admin/Dockerfile");
        appsCompose.ShouldContain("settings:");
    }

    private static bool IsApiAppsettings(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}Api{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Gateway.Api{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Coordinator.Api{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MicroServiceSystem.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}
