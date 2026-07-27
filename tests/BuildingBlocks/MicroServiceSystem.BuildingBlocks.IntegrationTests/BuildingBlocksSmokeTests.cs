using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Authentication;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.BuildingBlocks.Caching;
using MicroServiceSystem.BuildingBlocks.Caching.Configuration;
using MicroServiceSystem.BuildingBlocks.Messaging.Serialization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Results;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.BuildingBlocks.Storage.Configuration;
using MicroServiceSystem.BuildingBlocks.Storage.Providers;
using MicroServiceSystem.Contracts.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class BuildingBlocksSmokeTests
{
    [Fact]
    public void Password_hasher_roundtrips()
    {
        var hasher = new Pbkdf2PasswordHasher(Options.Create(new PasswordPolicyOptions()));

        string hash = hasher.Hash("Str0ng!Passw0rd");

        hasher.Verify("Str0ng!Passw0rd", hash).ShouldBe(PasswordVerificationResult.Success);
        hasher.Verify("wrong", hash).ShouldBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Jwt_token_service_issues_access_token()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "msf-tests",
            Audience = "msf-tests",
            SigningKey = "0123456789abcdef0123456789abcdef"
        });

        var tokenService = new JwtTokenService(options, new SystemDateTimeProvider());

        AccessToken token = tokenService.CreateAccessToken(new TokenSubject
        {
            UserId = Guid.NewGuid(),
            UserName = "tester",
            Email = "tester@example.com",
            TenantId = Guid.NewGuid(),
            Roles = ["Admin"],
            Permissions = ["users.read"]
        });

        token.Value.ShouldNotBeNullOrWhiteSpace();
        token.ExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Current_tenant_change_is_scoped()
    {
        var tenant = new CurrentTenant();
        Guid tenantId = Guid.NewGuid();

        using (tenant.Change(tenantId, "acme"))
        {
            tenant.Id.ShouldBe(tenantId);
            tenant.Name.ShouldBe("acme");
            tenant.IsAvailable.ShouldBeTrue();
        }

        tenant.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Cache_keys_are_tenant_scoped()
    {
        var tenant = new CurrentTenant();
        var builder = new CacheKeyBuilder(
            Options.Create(new CacheOptions { InstanceName = "tests" }),
            tenant);

        using (tenant.Change(Guid.Parse("11111111-1111-1111-1111-111111111111")))
        {
            builder.Build("users", "list").ShouldContain("11111111-1111-1111-1111-111111111111");
        }
    }

    [Fact]
    public void Integration_event_serializer_roundtrips()
    {
        var serializer = new IntegrationEventSerializer();
        var integrationEvent = new SampleIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOnUtc = DateTimeOffset.UtcNow,
            TenantId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString("N"),
            UserId = Guid.NewGuid()
        };

        var envelope = serializer.Serialize(integrationEvent, "identity");
        var restored = (SampleIntegrationEvent)serializer.Deserialize(envelope, typeof(SampleIntegrationEvent));

        restored.EventId.ShouldBe(integrationEvent.EventId);
        restored.UserId.ShouldBe(integrationEvent.UserId);
        envelope.Source.ShouldBe("identity");
        envelope.EventName.ShouldBe("identity.user_registered.v1");
    }

    [Fact]
    public async Task Local_file_storage_roundtrips()
    {
        string root = Path.Combine(Path.GetTempPath(), "msf-storage-" + Guid.NewGuid().ToString("N"));

        try
        {
            var storage = new LocalFileStorage(Options.Create(new FileStorageOptions
            {
                Local = new LocalStorageOptions { RootPath = root, PublicBaseUrl = "http://localhost/files" }
            }));

            await using var content = new MemoryStream("hello"u8.ToArray());

            StoredFile stored = await storage.UploadAsync(new FileUploadRequest
            {
                Container = "docs",
                Path = "readme.txt",
                Content = content,
                ContentType = "text/plain"
            }, TestContext.Current.CancellationToken);

            stored.SizeInBytes.ShouldBe(5);
            (await storage.ExistsAsync("docs", "readme.txt", TestContext.Current.CancellationToken)).ShouldBeTrue();

            FileDownload? download = await storage.DownloadAsync("docs", "readme.txt", TestContext.Current.CancellationToken);
            download.ShouldNotBeNull();

            await using Stream stream = download!.Content;
            using var reader = new StreamReader(stream);
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe("hello");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Result_error_types_map_to_http_status_codes()
    {
        ResultHttpMapper.ToStatusCode(ErrorType.NotFound).ShouldBe(404);
        ResultHttpMapper.ToStatusCode(ErrorType.Conflict).ShouldBe(409);
        ResultHttpMapper.ToStatusCode(ErrorType.Unauthorized).ShouldBe(401);
        ResultHttpMapper.ToStatusCode(ErrorType.Forbidden).ShouldBe(403);
    }

    [IntegrationEvent("identity.user_registered.v1")]
    private sealed record SampleIntegrationEvent : IntegrationEvent
    {
        public required Guid UserId { get; init; }
    }
}
